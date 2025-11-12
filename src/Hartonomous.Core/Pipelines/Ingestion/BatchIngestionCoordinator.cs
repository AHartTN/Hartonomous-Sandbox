using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Hartonomous.Core.Pipelines.Ingestion
{
    /// <summary>
    /// Orchestrates batch ingestion with Channel-based parallelism, rate limiting,
    /// progress tracking, and checkpoint/resume capabilities.
    /// </summary>
    public class BatchIngestionCoordinator
    {
        private readonly int _maxDegreeOfParallelism;
        private readonly int _channelCapacity;
        private readonly RateLimiter _rateLimiter;
        private readonly IIngestionCheckpointStore _checkpointStore;
        private readonly IProgressTracker _progressTracker;

        // Progress statistics
        private long _atomsProcessed;
        private long _atomsSucceeded;
        private long _atomsFailed;
        private DateTime _startTime;

        public BatchIngestionCoordinator(
            int maxDegreeOfParallelism = 4,
            int channelCapacity = 100,
            int rateLimit = 100, // atoms per second
            IIngestionCheckpointStore? checkpointStore = null,
            IProgressTracker? progressTracker = null)
        {
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
            _channelCapacity = channelCapacity;
            _rateLimiter = new RateLimiter(rateLimit);
            _checkpointStore = checkpointStore ?? new InMemoryCheckpointStore();
            _progressTracker = progressTracker ?? new ConsoleProgressTracker();
        }

        /// <summary>
        /// Processes atoms from source through the ingestion pipeline.
        /// Supports checkpoint/resume for fault tolerance.
        /// </summary>
        public async Task<BatchIngestionResult> IngestBatchAsync(
            Guid jobId,
            IAsyncEnumerable<BatchAtomRequest> sourceAtoms,
            Func<BatchAtomRequest, CancellationToken, Task<BatchAtomResponse>> processAtom,
            CancellationToken cancellationToken = default)
        {
            _startTime = DateTime.UtcNow;
            _atomsProcessed = 0;
            _atomsSucceeded = 0;
            _atomsFailed = 0;

            // Load checkpoint (if resuming)
            var checkpoint = await _checkpointStore.LoadCheckpointAsync(jobId);
            var processedIds = new HashSet<Guid>(checkpoint?.ProcessedAtomIds ?? Enumerable.Empty<Guid>());

            var channel = Channel.CreateBounded<BatchAtomRequest>(new BoundedChannelOptions(_channelCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

            var errors = new List<IngestionError>();

            // Producer: write atoms to channel (skip already processed)
            var producerTask = Task.Run(async () =>
            {
                try
                {
                    await foreach (var atom in sourceAtoms.WithCancellation(cancellationToken))
                    {
                        // Skip if already processed (checkpoint resume)
                        if (processedIds.Contains(atom.AtomId))
                        {
                            continue;
                        }

                        // Rate limiting
                        await _rateLimiter.WaitAsync(cancellationToken);

                        await channel.Writer.WriteAsync(atom, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    lock (errors)
                    {
                        errors.Add(new IngestionError
                        {
                            Message = "Producer error",
                            Exception = ex
                        });
                    }
                }
                finally
                {
                    channel.Writer.Complete();
                }
            }, cancellationToken);

            // Consumers: process atoms in parallel
            var consumerTasks = Enumerable.Range(0, _maxDegreeOfParallelism)
                .Select(async consumerId =>
                {
                    await foreach (var request in channel.Reader.ReadAllAsync(cancellationToken))
                    {
                        try
                        {
                            // Process atom
                            var response = await processAtom(request, cancellationToken);

                            Interlocked.Increment(ref _atomsProcessed);

                            if (response.Success)
                            {
                                Interlocked.Increment(ref _atomsSucceeded);

                                // Update checkpoint
                                await _checkpointStore.SaveCheckpointAsync(jobId, request.AtomId);
                            }
                            else
                            {
                                Interlocked.Increment(ref _atomsFailed);

                                lock (errors)
                                {
                                    errors.Add(new IngestionError
                                    {
                                        AtomId = request.AtomId,
                                        Message = response.ErrorMessage ?? "Unknown error",
                                        Exception = response.Exception
                                    });
                                }
                            }

                            // Report progress (every 10 atoms)
                            if (_atomsProcessed % 10 == 0)
                            {
                                await _progressTracker.ReportProgressAsync(new BatchIngestionProgress
                                {
                                    JobId = jobId,
                                    TotalProcessed = _atomsProcessed,
                                    Succeeded = _atomsSucceeded,
                                    Failed = _atomsFailed,
                                    AtomsPerSecond = ComputeAtomsPerSecond(),
                                    ElapsedSeconds = (DateTime.UtcNow - _startTime).TotalSeconds
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref _atomsProcessed);
                            Interlocked.Increment(ref _atomsFailed);

                            lock (errors)
                            {
                                errors.Add(new IngestionError
                                {
                                    AtomId = request.AtomId,
                                    Message = $"Consumer {consumerId} exception",
                                    Exception = ex
                                });
                            }
                        }
                    }
                })
                .ToList();

            // Wait for completion
            await producerTask;
            await Task.WhenAll(consumerTasks);

            // Final progress report
            await _progressTracker.ReportProgressAsync(new BatchIngestionProgress
            {
                JobId = jobId,
                TotalProcessed = _atomsProcessed,
                Succeeded = _atomsSucceeded,
                Failed = _atomsFailed,
                AtomsPerSecond = ComputeAtomsPerSecond(),
                ElapsedSeconds = (DateTime.UtcNow - _startTime).TotalSeconds,
                IsComplete = true
            });

            return new BatchIngestionResult
            {
                JobId = jobId,
                TotalProcessed = _atomsProcessed,
                Succeeded = _atomsSucceeded,
                Failed = _atomsFailed,
                Errors = errors,
                Duration = DateTime.UtcNow - _startTime
            };
        }

        private double ComputeAtomsPerSecond()
        {
            var elapsed = (DateTime.UtcNow - _startTime).TotalSeconds;
            return elapsed > 0 ? _atomsProcessed / elapsed : 0;
        }
    }

    /// <summary>
    /// Token bucket rate limiter for ingestion throughput control.
    /// </summary>
    public class RateLimiter
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly int _tokensPerSecond;
        private readonly Timer _timer;

        public RateLimiter(int tokensPerSecond)
        {
            _tokensPerSecond = tokensPerSecond;
            _semaphore = new SemaphoreSlim(tokensPerSecond, tokensPerSecond);
            
            // Refill tokens every second
            _timer = new Timer(_ =>
            {
                // Release up to max capacity
                var currentCount = _semaphore.CurrentCount;
                if (currentCount < tokensPerSecond)
                {
                    _semaphore.Release(tokensPerSecond - currentCount);
                }
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public async Task WaitAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Request to ingest an atom.
    /// </summary>
    public class BatchAtomRequest
    {
        public Guid AtomId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ContentUri { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Response from atom ingestion.
    /// </summary>
    public class BatchAtomResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// Result of batch ingestion job.
    /// </summary>
    public class BatchIngestionResult
    {
        public Guid JobId { get; set; }
        public long TotalProcessed { get; set; }
        public long Succeeded { get; set; }
        public long Failed { get; set; }
        public List<IngestionError> Errors { get; set; } = new List<IngestionError>();
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Error during ingestion.
    /// </summary>
    public class IngestionError
    {
        public Guid? AtomId { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// Progress update during ingestion.
    /// </summary>
    public class BatchIngestionProgress
    {
        public Guid JobId { get; set; }
        public long TotalProcessed { get; set; }
        public long Succeeded { get; set; }
        public long Failed { get; set; }
        public double AtomsPerSecond { get; set; }
        public double ElapsedSeconds { get; set; }
        public bool IsComplete { get; set; }
    }

    /// <summary>
    /// Interface for checkpoint storage (implement with SQL Server or Redis).
    /// </summary>
    public interface IIngestionCheckpointStore
    {
        Task<IngestionCheckpoint?> LoadCheckpointAsync(Guid jobId);
        Task SaveCheckpointAsync(Guid jobId, Guid atomId);
    }

    /// <summary>
    /// Checkpoint data for resuming ingestion jobs.
    /// </summary>
    public class IngestionCheckpoint
    {
        public Guid JobId { get; set; }
        public List<Guid> ProcessedAtomIds { get; set; } = new List<Guid>();
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// In-memory checkpoint store (development only).
    /// Replace with SQL Server (dbo.IngestionJobs table) in production.
    /// </summary>
    public class InMemoryCheckpointStore : IIngestionCheckpointStore
    {
        private readonly Dictionary<Guid, IngestionCheckpoint> _checkpoints = new Dictionary<Guid, IngestionCheckpoint>();

        public Task<IngestionCheckpoint?> LoadCheckpointAsync(Guid jobId)
        {
            _checkpoints.TryGetValue(jobId, out var checkpoint);
            return Task.FromResult(checkpoint);
        }

        public Task SaveCheckpointAsync(Guid jobId, Guid atomId)
        {
            if (!_checkpoints.ContainsKey(jobId))
            {
                _checkpoints[jobId] = new IngestionCheckpoint
                {
                    JobId = jobId,
                    ProcessedAtomIds = new List<Guid>()
                };
            }

            _checkpoints[jobId].ProcessedAtomIds.Add(atomId);
            _checkpoints[jobId].LastUpdated = DateTime.UtcNow;

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Interface for progress tracking (implement with SignalR for real-time UI updates).
    /// </summary>
    public interface IProgressTracker
    {
        Task ReportProgressAsync(BatchIngestionProgress progress);
    }

    /// <summary>
    /// Console-based progress tracker (development only).
    /// Replace with SignalR hub in production for real-time UI updates.
    /// </summary>
    public class ConsoleProgressTracker : IProgressTracker
    {
        public Task ReportProgressAsync(BatchIngestionProgress progress)
        {
            Console.WriteLine(
                $"[Job {progress.JobId}] Processed: {progress.TotalProcessed}, " +
                $"Succeeded: {progress.Succeeded}, Failed: {progress.Failed}, " +
                $"Rate: {progress.AtomsPerSecond:F2} atoms/sec, Elapsed: {progress.ElapsedSeconds:F1}s" +
                (progress.IsComplete ? " [COMPLETE]" : ""));

            return Task.CompletedTask;
        }
    }
}

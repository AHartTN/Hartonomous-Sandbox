using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Hartonomous.Core.Performance;

/// <summary>
/// High-performance parallel batch processing with work stealing.
/// Optimized for CPU-bound vector operations and data transformations.
/// </summary>
public static class BatchProcessor
{
    /// <summary>
    /// Process items in parallel batches with optimal chunking.
    /// Uses work stealing for load balancing.
    /// </summary>
    public static async ValueTask ProcessBatchAsync<T>(
        IReadOnlyList<T> items,
        Func<T, ValueTask> processAsync,
        int? maxDegreeOfParallelism = null,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0) return;

        int parallelism = maxDegreeOfParallelism ?? Environment.ProcessorCount;
        int chunkSize = Math.Max(1, items.Count / (parallelism * 4)); // 4x oversubscription for work stealing

        var chunks = ChunkItems(items, chunkSize);
        var tasks = new List<Task>(parallelism);

        using var semaphore = new SemaphoreSlim(parallelism);

        foreach (var chunk in chunks)
        {
            await semaphore.WaitAsync(cancellationToken);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    foreach (var item in chunk)
                    {
                        await processAsync(item);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Process items in parallel batches with transformation.
    /// Returns results preserving order.
    /// </summary>
    public static async ValueTask<TResult[]> ProcessBatchWithResultAsync<TSource, TResult>(
        IReadOnlyList<TSource> items,
        Func<TSource, ValueTask<TResult>> processAsync,
        int? maxDegreeOfParallelism = null,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0) return Array.Empty<TResult>();

        var results = new TResult[items.Count];
        int parallelism = maxDegreeOfParallelism ?? Environment.ProcessorCount;

        await Parallel.ForEachAsync(
            Enumerable.Range(0, items.Count),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = parallelism,
                CancellationToken = cancellationToken
            },
            async (i, ct) =>
            {
                results[i] = await processAsync(items[i]);
            });

        return results;
    }

    /// <summary>
    /// Process items in parallel batches (synchronous CPU-bound work).
    /// Optimized for vector operations and transformations.
    /// </summary>
    public static void ProcessBatchParallel<T>(
        T[] items,
        Action<T> process,
        int? maxDegreeOfParallelism = null)
    {
        if (items.Length == 0) return;

        int parallelism = maxDegreeOfParallelism ?? Environment.ProcessorCount;
        int chunkSize = Math.Max(1, items.Length / parallelism);

        Parallel.For(0, parallelism, i =>
        {
            int start = i * chunkSize;
            int end = (i == parallelism - 1) ? items.Length : start + chunkSize;

            for (int j = start; j < end; j++)
            {
                process(items[j]);
            }
        });
    }

    /// <summary>
    /// Process items with transformation (synchronous CPU-bound work).
    /// </summary>
    public static TResult[] ProcessBatchParallelWithResult<TSource, TResult>(
        TSource[] items,
        Func<TSource, TResult> transform,
        int? maxDegreeOfParallelism = null)
    {
        if (items.Length == 0) return Array.Empty<TResult>();

        var results = new TResult[items.Length];
        int parallelism = maxDegreeOfParallelism ?? Environment.ProcessorCount;

        Parallel.For(0, items.Length, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, i =>
        {
            results[i] = transform(items[i]);
        });

        return results;
    }

    /// <summary>
    /// Partition items for producer-consumer pattern.
    /// Returns concurrent queue for lock-free processing.
    /// </summary>
    public static ConcurrentQueue<T>[] PartitionForProducerConsumer<T>(
        IReadOnlyList<T> items,
        int partitionCount)
    {
        var partitions = new ConcurrentQueue<T>[partitionCount];
        for (int i = 0; i < partitionCount; i++)
        {
            partitions[i] = new ConcurrentQueue<T>();
        }

        for (int i = 0; i < items.Count; i++)
        {
            partitions[i % partitionCount].Enqueue(items[i]);
        }

        return partitions;
    }

    /// <summary>
    /// Batch items into chunks for processing.
    /// Efficient for database operations and API calls.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T[]> ChunkItems<T>(IReadOnlyList<T> items, int chunkSize)
    {
        for (int i = 0; i < items.Count; i += chunkSize)
        {
            int size = Math.Min(chunkSize, items.Count - i);
            var chunk = new T[size];
            for (int j = 0; j < size; j++)
            {
                chunk[j] = items[i + j];
            }
            yield return chunk;
        }
    }

    /// <summary>
    /// Process batches with rate limiting.
    /// Useful for API calls with rate limits.
    /// </summary>
    public static async ValueTask ProcessWithRateLimitAsync<T>(
        IReadOnlyList<T> items,
        Func<T, ValueTask> processAsync,
        int maxItemsPerSecond,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0 || maxItemsPerSecond <= 0) return;

        var interval = TimeSpan.FromMilliseconds(1000.0 / maxItemsPerSecond);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < items.Count; i++)
        {
            await processAsync(items[i]);

            // Throttle if needed
            var expectedTime = TimeSpan.FromMilliseconds(interval.TotalMilliseconds * (i + 1));
            var actualTime = stopwatch.Elapsed;
            if (actualTime < expectedTime)
            {
                await Task.Delay(expectedTime - actualTime, cancellationToken);
            }
        }
    }
}

/// <summary>
/// Async batch processing with configurable pipeline stages.
/// Optimized for multi-stage data transformations.
/// </summary>
/// <typeparam name="T">Item type</typeparam>
public sealed class PipelineBatchProcessor<T> : IDisposable
{
    private readonly Channel<T> _channel;
    private readonly int _maxDegreeOfParallelism;
    private readonly List<Func<T, ValueTask<T>>> _stages = new();

    public PipelineBatchProcessor(int capacity = 1000, int? maxDegreeOfParallelism = null)
    {
        _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
        _maxDegreeOfParallelism = maxDegreeOfParallelism ?? Environment.ProcessorCount;
    }

    /// <summary>
    /// Add a processing stage to the pipeline.
    /// </summary>
    public PipelineBatchProcessor<T> AddStage(Func<T, ValueTask<T>> stage)
    {
        _stages.Add(stage);
        return this;
    }

    /// <summary>
    /// Process all items through the pipeline.
    /// </summary>
    public async ValueTask<List<T>> ProcessAsync(
        IReadOnlyList<T> items,
        CancellationToken cancellationToken = default)
    {
        var results = new ConcurrentBag<T>();

        // Producer
        var producerTask = Task.Run(async () =>
        {
            foreach (var item in items)
            {
                await _channel.Writer.WriteAsync(item, cancellationToken);
            }
            _channel.Writer.Complete();
        }, cancellationToken);

        // Consumers (parallel processing)
        var consumerTasks = Enumerable.Range(0, _maxDegreeOfParallelism)
            .Select(_ => Task.Run(async () =>
            {
                await foreach (var item in _channel.Reader.ReadAllAsync(cancellationToken))
                {
                    var processed = item;
                    foreach (var stage in _stages)
                    {
                        processed = await stage(processed);
                    }
                    results.Add(processed);
                }
            }, cancellationToken))
            .ToArray();

        await Task.WhenAll(producerTask);
        await Task.WhenAll(consumerTasks);

        return results.ToList();
    }

    public void Dispose()
    {
        // Channel is struct, no disposal needed
    }
}

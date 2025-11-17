using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hartonomous.Data;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Jobs;

/// <summary>
/// Coordinates execution of background jobs with retry logic, error handling, and telemetry.
/// </summary>
public class JobExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobExecutor> _logger;
    private readonly Dictionary<string, Type> _processorTypes = new();

    public JobExecutor(
        IServiceProvider serviceProvider,
        ILogger<JobExecutor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Registers a job processor type for a specific job type identifier.
    /// </summary>
    public void RegisterProcessor<TPayload, TProcessor>(string jobType)
        where TPayload : class
        where TProcessor : IJobProcessor<TPayload>
    {
        _processorTypes[jobType] = typeof(TProcessor);
        _logger.LogInformation("Registered job processor {ProcessorType} for job type '{JobType}'",
            typeof(TProcessor).Name, jobType);
    }

    /// <summary>
    /// Executes a background job by ID with retry logic and telemetry.
    /// </summary>
    /// <param name="jobId">Job ID to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if job completed successfully, false if failed.</returns>
    public async Task<bool> ExecuteJobAsync(long jobId, CancellationToken cancellationToken)
    {
        // Create a scope to resolve scoped services like DbContext
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HartonomousDbContext>();
        
        var stopwatch = Stopwatch.StartNew();
        BackgroundJobs? job = null;

        try
        {
            // Load job
            job = await dbContext.BackgroundJobs
                .FirstOrDefaultAsync(j => j.JobId == jobId, cancellationToken);

            if (job == null)
            {
                _logger.LogWarning("Job {JobId} not found", jobId);
                return false;
            }

            // Check if already completed/cancelled
            if (job.Status == (int)JobStatus.Completed || job.Status == (int)JobStatus.Cancelled)
            {
                _logger.LogInformation("Job {JobId} already in terminal state: {Status}", jobId, job.Status);
                return job.Status == (int)JobStatus.Completed;
            }

            // Check if scheduled for future
            if (job.ScheduledAtUtc.HasValue && job.ScheduledAtUtc.Value > DateTime.UtcNow)
            {
                _logger.LogDebug("Job {JobId} scheduled for {ScheduledAt}, skipping", jobId, job.ScheduledAtUtc.Value);
                return false;
            }

            // Check retry attempts
            if (job.AttemptCount >= job.MaxRetries)
            {
                _logger.LogWarning("Job {JobId} exceeded max retries ({MaxRetries}), dead lettering",
                    jobId, job.MaxRetries);
                await DeadLetterJobAsync(job, dbContext, "Maximum retry attempts exceeded", cancellationToken);
                return false;
            }

            // Mark in progress
            job.Status = (int)JobStatus.InProgress;
            job.AttemptCount++;
            job.StartedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Executing job {JobId} (type: {JobType}, attempt: {Attempt}/{MaxRetries})",
                jobId, job.JobType, job.AttemptCount, job.MaxRetries);

            // Get processor from scoped service provider
            if (!_processorTypes.TryGetValue(job.JobType, out var processorType))
            {
                throw new InvalidOperationException($"No processor registered for job type '{job.JobType}'");
            }

            var processor = scope.ServiceProvider.GetService(processorType);
            if (processor == null)
            {
                throw new InvalidOperationException($"Failed to resolve processor of type '{processorType.Name}'");
            }

            // Create execution context
            var context = new JobExecutionContext
            {
                JobId = job.JobId,
                AttemptNumber = job.AttemptCount,
                MaxRetries = job.MaxRetries,
                TenantId = job.TenantId,
                CorrelationId = job.CorrelationId,
                CreatedBy = job.CreatedBy,
                CreatedAtUtc = job.CreatedAtUtc
            };

            // Deserialize payload and invoke processor
            var payloadType = processorType.GetInterface(typeof(IJobProcessor<>).Name)?.GetGenericArguments()[0]
                ?? throw new InvalidOperationException($"Processor {processorType.Name} does not implement IJobProcessor<>");

            object? payload = null;
            if (!string.IsNullOrEmpty(job.Payload))
            {
                payload = JsonSerializer.Deserialize(job.Payload, payloadType);
            }

            var processMethod = processorType.GetMethod(nameof(IJobProcessor<object>.ProcessAsync))
                ?? throw new InvalidOperationException($"ProcessAsync method not found on {processorType.Name}");

            var resultTask = (Task<object?>)processMethod.Invoke(processor, new[] { payload, context, cancellationToken })!;
            var result = await resultTask;

            // Mark completed
            job.Status = (int)JobStatus.Completed;
            job.CompletedAtUtc = DateTime.UtcNow;
            job.ResultData = result != null ? JsonSerializer.Serialize(result) : null;
            job.ErrorMessage = null;
            job.ErrorStackTrace = null;

            await dbContext.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation("Job {JobId} completed successfully in {ElapsedMs}ms",
                jobId, stopwatch.ElapsedMilliseconds);

            return true;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Job {JobId} failed after {ElapsedMs}ms (attempt {Attempt}): {Message}",
                jobId, stopwatch.ElapsedMilliseconds, job?.AttemptCount ?? 0, ex.Message);

            if (job != null)
            {
                await MarkJobFailedAsync(job, dbContext, ex, cancellationToken);
            }

            return false;
        }
    }

    /// <summary>
    /// Marks a job as failed and schedules retry if attempts remain.
    /// </summary>
    private async Task MarkJobFailedAsync(BackgroundJobs job, HartonomousDbContext context, Exception exception, CancellationToken cancellationToken)
    {
        job.Status = (int)JobStatus.Failed;
        job.ErrorMessage = exception.Message;
        job.ErrorStackTrace = exception.StackTrace;
        job.CompletedAtUtc = DateTime.UtcNow;

        // Check if should dead letter
        if (job.AttemptCount >= job.MaxRetries)
        {
            job.Status = (int)JobStatus.DeadLettered;
            _logger.LogWarning("Job {JobId} dead lettered after {Attempts} attempts", job.JobId, job.AttemptCount);
        }
        else
        {
            // Calculate exponential backoff delay
            var delaySeconds = Math.Pow(2, job.AttemptCount - 1); // 1s, 2s, 4s, 8s, ...
            job.ScheduledAtUtc = DateTime.UtcNow.AddSeconds(delaySeconds);
            job.Status = (int)JobStatus.Pending; // Reset to pending for retry

            _logger.LogInformation("Job {JobId} will retry in {DelaySeconds}s (attempt {Attempt}/{MaxRetries})",
                job.JobId, delaySeconds, job.AttemptCount, job.MaxRetries);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Moves a job to dead letter state with a reason.
    /// </summary>
    private async Task DeadLetterJobAsync(BackgroundJobs job, HartonomousDbContext context, string reason, CancellationToken cancellationToken)
    {
        job.Status = (int)JobStatus.DeadLettered;
        job.ErrorMessage = reason;
        job.CompletedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }
}

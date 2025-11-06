using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Hartonomous.Data;

namespace Hartonomous.Infrastructure.Jobs;

/// <summary>
/// Background service that continuously polls for pending jobs and executes them.
/// Supports priority-based processing and configurable concurrency.
/// </summary>
public class BackgroundJobWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundJobWorker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(2);
    private readonly int _batchSize = 10;
    private readonly SemaphoreSlim _concurrencySemaphore;

    public BackgroundJobWorker(
        IServiceProvider serviceProvider,
        ILogger<BackgroundJobWorker> logger,
        int maxConcurrency = 5)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _concurrencySemaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackgroundJobWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessJobBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing job batch: {Message}", ex.Message);
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("BackgroundJobWorker stopping");
    }

    /// <summary>
    /// Processes a batch of pending jobs with priority ordering.
    /// </summary>
    private async Task ProcessJobBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HartonomousDbContext>();

        // Query pending jobs ordered by priority (desc) and created time (asc)
        var pendingJobs = await context.Set<BackgroundJob>()
            .Where(j => j.Status == JobStatus.Pending &&
                       (j.ScheduledAtUtc == null || j.ScheduledAtUtc <= DateTime.UtcNow))
            .OrderByDescending(j => j.Priority)
            .ThenBy(j => j.CreatedAtUtc)
            .Take(_batchSize)
            .Select(j => j.JobId)
            .ToListAsync(cancellationToken);

        if (pendingJobs.Count == 0)
        {
            return; // No jobs to process
        }

        _logger.LogDebug("Found {Count} pending jobs to process", pendingJobs.Count);

        // Process jobs concurrently (up to maxConcurrency)
        var tasks = pendingJobs.Select(jobId => ProcessJobWithSemaphoreAsync(jobId, cancellationToken));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Processes a single job with semaphore-based concurrency control.
    /// </summary>
    private async Task ProcessJobWithSemaphoreAsync(long jobId, CancellationToken cancellationToken)
    {
        await _concurrencySemaphore.WaitAsync(cancellationToken);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var executor = scope.ServiceProvider.GetRequiredService<JobExecutor>();
            await executor.ExecuteJobAsync(jobId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing job {JobId}: {Message}", jobId, ex.Message);
        }
        finally
        {
            _concurrencySemaphore.Release();
        }
    }

    public override void Dispose()
    {
        _concurrencySemaphore.Dispose();
        base.Dispose();
    }
}

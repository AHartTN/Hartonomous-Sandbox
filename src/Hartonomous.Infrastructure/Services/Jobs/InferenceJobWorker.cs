using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Services.Jobs;

/// <summary>
/// Background service that continuously polls for pending inference jobs and processes them.
/// Runs on a 2-second polling interval and processes up to 10 jobs per iteration.
/// </summary>
public sealed class InferenceJobWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InferenceJobWorker> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Initializes a new instance of the <see cref="InferenceJobWorker"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for creating scoped services per job batch.</param>
    /// <param name="logger">Logger for tracking worker lifecycle and errors.</param>
    public InferenceJobWorker(IServiceProvider serviceProvider, ILogger<InferenceJobWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Executes the background polling loop, processing pending jobs every 2 seconds until cancellation is requested.
    /// </summary>
    /// <param name="stoppingToken">Token to signal when the worker should stop.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Inference job worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing inference jobs");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("Inference job worker stopped");
    }

    /// <summary>
    /// Queries for up to 10 pending inference requests (ordered by timestamp) and processes each via InferenceJobProcessor.
    /// Creates a new scope for each batch to ensure proper dependency lifetime management.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private async Task ProcessPendingJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HartonomousDbContext>();
        var processor = scope.ServiceProvider.GetRequiredService<InferenceJobProcessor>();

        var pendingJobs = await context.InferenceRequests
            .Where(r => r.Status == "Pending")
            .OrderBy(r => r.RequestTimestamp)
            .Take(10)
            .Select(r => r.InferenceId)
            .ToListAsync(cancellationToken);

        foreach (var jobId in pendingJobs)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await processor.ProcessJobAsync(jobId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process job {JobId}", jobId);
            }
        }
    }
}

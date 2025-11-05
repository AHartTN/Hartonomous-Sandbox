using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services.Jobs;

public sealed class InferenceJobWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InferenceJobWorker> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(2);

    public InferenceJobWorker(IServiceProvider serviceProvider, ILogger<InferenceJobWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

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

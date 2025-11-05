using System.Linq;
using Hartonomous.Admin.Hubs;
using Hartonomous.Admin.Models;
using Hartonomous.Admin.Operations;
using Hartonomous.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hartonomous.Admin.Services;

public sealed class TelemetryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AdminOperationCoordinator _operationCoordinator;
    private readonly AdminTelemetryCache _cache;
    private readonly IHubContext<TelemetryHub> _hubContext;
    private readonly IOptionsMonitor<AdminTelemetryOptions> _options;
    private readonly ILogger<TelemetryBackgroundService> _logger;

    public TelemetryBackgroundService(
        IServiceScopeFactory scopeFactory,
        AdminOperationCoordinator operationCoordinator,
        AdminTelemetryCache cache,
        IHubContext<TelemetryHub> hubContext,
        IOptionsMonitor<AdminTelemetryOptions> options,
        ILogger<TelemetryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _operationCoordinator = operationCoordinator;
        _cache = cache;
        _hubContext = hubContext;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Telemetry background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var statisticsService = scope.ServiceProvider.GetRequiredService<IIngestionStatisticsService>();
                
                var stats = await statisticsService.GetStatsAsync(stoppingToken).ConfigureAwait(false);
                var operations = _operationCoordinator.GetRecent();
                var snapshot = new AdminDashboardSnapshot(
                    stats.TotalModels,
                    stats.TotalParameters,
                    stats.TotalLayers,
                    stats.ArchitectureBreakdown,
                    operations.ToList(),
                    DateTimeOffset.UtcNow);

                _cache.SetSnapshot(snapshot);

                await _hubContext.Clients.Group("metrics")
                    .SendAsync("metricsUpdated", snapshot, cancellationToken: stoppingToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Failed to refresh telemetry snapshot");
            }

            var period = Math.Max(1, _options.CurrentValue.PollIntervalSeconds);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(period), stoppingToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Telemetry background service stopped");
    }
}

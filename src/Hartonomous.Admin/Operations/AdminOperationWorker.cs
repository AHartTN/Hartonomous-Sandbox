using Hartonomous.Admin.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Admin.Operations;

public sealed class AdminOperationWorker : BackgroundService
{
    private readonly AdminOperationCoordinator _coordinator;
    private readonly IHubContext<TelemetryHub> _hubContext;
    private readonly ILogger<AdminOperationWorker> _logger;

    public AdminOperationWorker(
        AdminOperationCoordinator coordinator,
        IHubContext<TelemetryHub> hubContext,
        ILogger<AdminOperationWorker> logger)
    {
        _coordinator = coordinator;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Admin operation worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            AdminOperationCoordinator.AdminOperationExecution execution;

            try
            {
                execution = await _coordinator.DequeueAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var status = execution.Status;
            var running = status with
            {
                State = AdminOperationState.Running,
                StartedAt = DateTimeOffset.UtcNow,
                Detail = "Running"
            };

            _coordinator.Publish(running);
            await BroadcastAsync(running, stoppingToken).ConfigureAwait(false);

            try
            {
                var outcome = await execution.Work(stoppingToken).ConfigureAwait(false);

                var completed = running with
                {
                    State = outcome.Success ? AdminOperationState.Succeeded : AdminOperationState.Failed,
                    CompletedAt = DateTimeOffset.UtcNow,
                    Detail = outcome.Message,
                    Error = outcome.Success ? null : outcome.Error ?? outcome.Message
                };

                _coordinator.Publish(completed);
                await BroadcastAsync(completed, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Operation {OperationId} failed", status.OperationId);

                var failed = running with
                {
                    State = AdminOperationState.Failed,
                    CompletedAt = DateTimeOffset.UtcNow,
                    Detail = "Operation failed",
                    Error = ex.Message
                };

                _coordinator.Publish(failed);
                await BroadcastAsync(failed, stoppingToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Admin operation worker stopped");
    }

    private Task BroadcastAsync(AdminOperationStatus status, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.Group("operations").SendAsync("operationUpdated", status, cancellationToken);
    }
}

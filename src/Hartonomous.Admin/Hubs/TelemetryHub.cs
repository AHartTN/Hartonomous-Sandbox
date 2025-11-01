using Hartonomous.Admin.Operations;
using Hartonomous.Admin.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Admin.Hubs;

public sealed class TelemetryHub : Hub
{
    private readonly AdminTelemetryCache _cache;
    private readonly AdminOperationCoordinator _operationCoordinator;
    private readonly ILogger<TelemetryHub> _logger;

    public TelemetryHub(
        AdminTelemetryCache cache,
        AdminOperationCoordinator operationCoordinator,
        ILogger<TelemetryHub> logger)
    {
        _cache = cache;
        _operationCoordinator = operationCoordinator;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync().ConfigureAwait(false);

        _logger.LogInformation("Client {ConnectionId} connected to telemetry hub", Context.ConnectionId);

        await Groups.AddToGroupAsync(Context.ConnectionId, "metrics").ConfigureAwait(false);
        await Groups.AddToGroupAsync(Context.ConnectionId, "operations").ConfigureAwait(false);

        await Clients.Caller.SendAsync("metricsUpdated", _cache.Snapshot).ConfigureAwait(false);
        await Clients.Caller.SendAsync("operationsSnapshot", _operationCoordinator.GetRecent()).ConfigureAwait(false);
    }

    public Task SubscribeToMetrics() => Groups.AddToGroupAsync(Context.ConnectionId, "metrics");

    public Task SubscribeToOperations() => Groups.AddToGroupAsync(Context.ConnectionId, "operations");
}

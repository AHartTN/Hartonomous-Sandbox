using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Hartonomous.Infrastructure.Services.SignalR;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// SignalR hub for real-time streaming ingestion progress updates.
/// </summary>
public class IngestionHub : Hub
{
    private readonly ILogger<IngestionHub> _logger;

    public IngestionHub(ILogger<IngestionHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client subscribes to a specific streaming session.
    /// </summary>
    public async Task SubscribeToSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        _logger.LogDebug("Client {ConnectionId} subscribed to session {SessionId}", Context.ConnectionId, sessionId);
    }

    /// <summary>
    /// Client unsubscribes from a streaming session.
    /// </summary>
    public async Task UnsubscribeFromSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
        _logger.LogDebug("Client {ConnectionId} unsubscribed from session {SessionId}", Context.ConnectionId, sessionId);
    }
}

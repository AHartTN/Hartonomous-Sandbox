using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.SignalR;

/// <summary>
/// Stub base class for SignalR hubs. Will be replaced with actual SignalR implementation in API layer.
/// </summary>
public abstract class Hub
{
    public HubCallerContext Context { get; internal set; } = new HubCallerContext();
    public IGroupManager Groups { get; internal set; } = new GroupManager();

    public virtual Task OnConnectedAsync() => Task.CompletedTask;
    public virtual Task OnDisconnectedAsync(System.Exception? exception) => Task.CompletedTask;
}

/// <summary>
/// Stub for SignalR hub context
/// </summary>
public class HubCallerContext
{
    public string ConnectionId { get; set; } = string.Empty;
}

/// <summary>
/// Stub for SignalR group manager
/// </summary>
public interface IGroupManager
{
    Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default);
    Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default);
}

internal class GroupManager : IGroupManager
{
    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

/// <summary>
/// Stub for SignalR hub context
/// </summary>
public interface IHubContext<THub> where THub : Hub
{
    IHubClients Clients { get; }
}

/// <summary>
/// Stub for hub clients
/// </summary>
public interface IHubClients
{
    IClientProxy Client(string connectionId);
}

/// <summary>
/// Stub for client proxy
/// </summary>
public interface IClientProxy
{
    Task SendAsync(string method, object? arg1, CancellationToken cancellationToken = default);
    Task SendAsync(string method, object? arg1, object? arg2, CancellationToken cancellationToken = default);
}

/// <summary>
/// Stub implementation of hub context for testing
/// </summary>
internal class StubHubContext<THub> : IHubContext<THub> where THub : Hub
{
    public IHubClients Clients { get; } = new StubHubClients();
}

internal class StubHubClients : IHubClients
{
    public IClientProxy Client(string connectionId) => new StubClientProxy();
}

internal class StubClientProxy : IClientProxy
{
    public Task SendAsync(string method, object? arg1, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SendAsync(string method, object? arg1, object? arg2, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

using Hartonomous.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Services;

public abstract class BaseService : IService
{
    protected ILogger Logger { get; }

    protected BaseService(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public abstract string ServiceName { get; }

    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("{ServiceName} initializing...", ServiceName);
        await Task.CompletedTask;
        Logger.LogInformation("{ServiceName} initialized", ServiceName);
    }

    public virtual async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(true);
    }

    protected void LogDebug(string message, params object[] args)
    {
        Logger.LogDebug(message, args);
    }

    protected void LogInformation(string message, params object[] args)
    {
        Logger.LogInformation(message, args);
    }

    protected void LogWarning(string message, params object[] args)
    {
        Logger.LogWarning(message, args);
    }

    protected void LogError(Exception exception, string message, params object[] args)
    {
        Logger.LogError(exception, message, args);
    }
}

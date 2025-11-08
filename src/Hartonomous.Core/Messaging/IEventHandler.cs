using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Messaging;

/// <summary>
/// Generic event handler interface for type-safe event processing.
/// Provides consistent handling pattern for domain and infrastructure events.
/// </summary>
/// <typeparam name="TEvent">The event type this handler processes.</typeparam>
public interface IEventHandler<in TEvent> where TEvent : class
{
    /// <summary>
    /// Handles the specified event asynchronously.
    /// </summary>
    /// <param name="event">The event to handle.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for event handlers with common logging and error handling infrastructure.
/// Reduces boilerplate in concrete handler implementations.
/// </summary>
/// <typeparam name="TEvent">The event type this handler processes.</typeparam>
public abstract class EventHandlerBase<TEvent> : IEventHandler<TEvent> where TEvent : class
{
    /// <summary>
    /// Executes the handler with exception handling and telemetry.
    /// </summary>
    public async Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));

        try
        {
            await ExecuteCoreAsync(@event, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Graceful cancellation - log at debug level
            LogDebug("Event handling cancelled", @event);
            throw;
        }
        catch (Exception ex)
        {
            // Log error with event context
            LogError(ex, "Event handling failed", @event);
            throw;
        }
    }

    /// <summary>
    /// Implement event-specific handling logic here.
    /// Exception handling is managed by the base class.
    /// </summary>
    protected abstract Task ExecuteCoreAsync(TEvent @event, CancellationToken cancellationToken);

    /// <summary>
    /// Log informational message with event context.
    /// Override to customize logging behavior.
    /// </summary>
    protected abstract void LogInformation(string message, TEvent @event);

    /// <summary>
    /// Log warning message with event context.
    /// Override to customize logging behavior.
    /// </summary>
    protected abstract void LogWarning(string message, TEvent @event);

    /// <summary>
    /// Log error with exception and event context.
    /// Override to customize logging behavior.
    /// </summary>
    protected abstract void LogError(Exception ex, string message, TEvent @event);

    /// <summary>
    /// Log debug message with event context.
    /// Override to customize logging behavior.
    /// </summary>
    protected abstract void LogDebug(string message, TEvent @event);
}

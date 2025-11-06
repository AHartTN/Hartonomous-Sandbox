namespace Hartonomous.Infrastructure.Messaging;

/// <summary>
/// Event bus abstraction for publish/subscribe messaging.
/// Supports Azure Service Bus, RabbitMQ, or in-memory implementations.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event to a topic.
    /// </summary>
    /// <typeparam name="TEvent">Event type.</typeparam>
    /// <param name="event">Event to publish.</param>
    /// <param name="topicName">Topic name (null = derive from event type).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<TEvent>(
        TEvent @event,
        string? topicName = null,
        CancellationToken cancellationToken = default) where TEvent : class;

    /// <summary>
    /// Publishes multiple events as a batch.
    /// </summary>
    Task PublishBatchAsync<TEvent>(
        IEnumerable<TEvent> events,
        string? topicName = null,
        CancellationToken cancellationToken = default) where TEvent : class;

    /// <summary>
    /// Subscribes to events of a specific type.
    /// </summary>
    /// <typeparam name="TEvent">Event type.</typeparam>
    /// <param name="handler">Event handler.</param>
    /// <param name="subscriptionName">Subscription name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SubscribeAsync<TEvent>(
        Func<TEvent, CancellationToken, Task> handler,
        string subscriptionName,
        CancellationToken cancellationToken = default) where TEvent : class;

    /// <summary>
    /// Starts processing messages for all registered subscriptions.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops processing messages.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}

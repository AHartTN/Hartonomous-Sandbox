using Hartonomous.Core.Messaging;

namespace Hartonomous.Core.Abstracts;

/// <summary>
/// Unified contract for publishing and consuming platform messages.
/// Backing implementations may use SQL Service Broker, queues, or other transports.
/// </summary>
public interface IMessageBroker
{
    /// <summary>
    /// Publishes a single message payload to the broker.
    /// Implementations should persist the message durably before returning.
    /// </summary>
    Task PublishAsync<TPayload>(TPayload payload, CancellationToken cancellationToken = default)
        where TPayload : class;

    /// <summary>
    /// Publishes multiple payloads as an atomic batch when supported.
    /// When batching is not supported, the implementation may fan out and send sequentially.
    /// </summary>
    Task PublishBatchAsync<TPayload>(IEnumerable<TPayload> payloads, CancellationToken cancellationToken = default)
        where TPayload : class;

    /// <summary>
    /// Receives a single message from the broker, waiting up to <paramref name="waitTime"/>.
    /// Returns <c>null</c> when no message is available in the allotted time.
    /// The caller is responsible for completing or abandoning the returned message.
    /// </summary>
    Task<BrokeredMessage?> ReceiveAsync(TimeSpan waitTime, CancellationToken cancellationToken = default);
}

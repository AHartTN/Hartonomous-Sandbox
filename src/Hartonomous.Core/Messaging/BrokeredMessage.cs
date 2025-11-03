using System.Threading;
using Hartonomous.Core.Serialization;

namespace Hartonomous.Core.Messaging;

/// <summary>
/// Represents a message retrieved from the broker. Callers must complete or abandon the message
/// to finalize processing. Disposing the message abandons it if still pending.
/// </summary>
public sealed class BrokeredMessage : IAsyncDisposable
{
    private readonly Func<CancellationToken, Task> _completeAsync;
    private readonly Func<CancellationToken, Task> _abandonAsync;
    private readonly IJsonSerializer _serializer;
    private int _state; // 0 = pending, 1 = completed, 2 = abandoned/disposing

    public BrokeredMessage(
        Guid conversationHandle,
        string messageType,
        string? body,
    DateTimeOffset enqueueTime,
    Func<CancellationToken, Task> completeAsync,
    Func<CancellationToken, Task> abandonAsync,
    IJsonSerializer? serializer = null)
    {
        ConversationHandle = conversationHandle;
        MessageType = messageType;
        Body = body;
        EnqueueTime = enqueueTime;
        _completeAsync = completeAsync ?? throw new ArgumentNullException(nameof(completeAsync));
        _abandonAsync = abandonAsync ?? throw new ArgumentNullException(nameof(abandonAsync));
    _serializer = serializer ?? new SystemTextJsonSerializer();
    }

    /// <summary>
    /// Identifies the Service Broker conversation associated with this message.
    /// </summary>
    public Guid ConversationHandle { get; }

    /// <summary>
    /// Logical message type name supplied by the broker implementation.
    /// </summary>
    public string MessageType { get; }

    /// <summary>
    /// Raw message body as a JSON payload. May be null when the transport delivered an empty body.
    /// </summary>
    public string? Body { get; }

    /// <summary>
    /// Timestamp recorded by the broker when the message was enqueued.
    /// </summary>
    public DateTimeOffset EnqueueTime { get; }

    /// <summary>
    /// Attempts to deserialize the JSON body into the specified type.
    /// </summary>
    public TPayload? Deserialize<TPayload>()
    {
        if (Body is null)
        {
            return default;
        }

        return _serializer.Deserialize<TPayload>(Body);
    }

    /// <summary>
    /// Commits the message, permanently removing it from the queue.
    /// </summary>
    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
        {
            return Task.CompletedTask;
        }

        return _completeAsync(cancellationToken);
    }

    /// <summary>
    /// Abandons the message, returning it to the queue for reprocessing.
    /// </summary>
    public Task AbandonAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _state, 2, 0) != 0)
        {
            return Task.CompletedTask;
        }

        return _abandonAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _state, 2, 0) == 0)
        {
            await _abandonAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}

using System;
using System.Collections.Generic;

namespace Hartonomous.Core.Messaging;

public sealed class DeadLetterMessage
{
    public Guid ConversationHandle { get; init; }

    public string MessageType { get; init; } = string.Empty;

    public string? Body { get; init; }

    public DateTimeOffset EnqueueTime { get; init; }

    public int AttemptCount { get; init; }

    public string Reason { get; init; } = string.Empty;

    public string? ExceptionType { get; init; }

    public string? ExceptionMessage { get; init; }

    public string? ExceptionStackTrace { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

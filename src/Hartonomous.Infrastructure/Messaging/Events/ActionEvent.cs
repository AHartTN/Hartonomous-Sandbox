namespace Hartonomous.Infrastructure.Messaging.Events;

/// <summary>
/// OODA Loop: Action event - execution of chosen action.
/// </summary>
public class ActionEvent : IntegrationEvent
{
    /// <summary>
    /// Decision that triggered this action.
    /// </summary>
    public required Guid DecisionId { get; init; }

    /// <summary>
    /// Action type (inference, generation, indexing, alert).
    /// </summary>
    public required string ActionType { get; init; }

    /// <summary>
    /// Action status (initiated, in_progress, completed, failed).
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Result of the action.
    /// </summary>
    public object? Result { get; init; }

    /// <summary>
    /// Error details if action failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; init; }
}

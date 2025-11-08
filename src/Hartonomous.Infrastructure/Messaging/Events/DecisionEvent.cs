namespace Hartonomous.Infrastructure.Messaging.Events;

/// <summary>
/// OODA Loop: Decision event - action selection based on orientation.
/// </summary>
public class DecisionEvent : IntegrationEvent
{
    /// <summary>
    /// Decision identifier.
    /// </summary>
    public required Guid DecisionId { get; init; }

    /// <summary>
    /// Chosen action.
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Confidence score (0-1).
    /// </summary>
    public required float Confidence { get; init; }

    /// <summary>
    /// Model or rule that made the decision.
    /// </summary>
    public string? DecisionMaker { get; init; }

    /// <summary>
    /// Alternative actions considered.
    /// </summary>
    public List<string>? Alternatives { get; init; }

    /// <summary>
    /// Reasoning or explanation.
    /// </summary>
    public string? Reasoning { get; init; }
}

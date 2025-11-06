namespace Hartonomous.Infrastructure.Messaging.Events;

/// <summary>
/// OODA Loop: Observation event - new data ingested into the system.
/// </summary>
public class ObservationEvent : IntegrationEvent
{
    /// <summary>
    /// ID of the ingested atom.
    /// </summary>
    public required long AtomId { get; init; }

    /// <summary>
    /// Source type (text, image, audio, video, scada).
    /// </summary>
    public required string SourceType { get; init; }

    /// <summary>
    /// Embedding ID if generated.
    /// </summary>
    public long? EmbeddingId { get; init; }

    /// <summary>
    /// Metadata about the observation.
    /// </summary>
    public string? Metadata { get; init; }
}

/// <summary>
/// OODA Loop: Orientation event - pattern recognition, clustering, correlation.
/// </summary>
public class OrientationEvent : IntegrationEvent
{
    /// <summary>
    /// Atoms involved in the orientation.
    /// </summary>
    public required List<long> AtomIds { get; init; }

    /// <summary>
    /// Detected patterns or clusters.
    /// </summary>
    public Dictionary<string, object>? Patterns { get; init; }

    /// <summary>
    /// Similarity scores between atoms.
    /// </summary>
    public Dictionary<string, float>? Similarities { get; init; }

    /// <summary>
    /// Orientation type (clustering, correlation, anomaly_detection).
    /// </summary>
    public required string OrientationType { get; init; }
}

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

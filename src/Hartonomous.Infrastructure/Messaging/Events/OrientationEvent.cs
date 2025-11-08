namespace Hartonomous.Infrastructure.Messaging.Events;

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

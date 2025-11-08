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

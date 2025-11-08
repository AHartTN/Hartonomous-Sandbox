namespace Hartonomous.Infrastructure.Messaging.Events;

/// <summary>
/// Event published when an atom is ingested.
/// </summary>
public class AtomIngestedEvent : IntegrationEvent
{
    public required long AtomId { get; init; }
    public required string ContentType { get; init; }
    public long? EmbeddingId { get; init; }
}

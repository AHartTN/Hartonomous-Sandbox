namespace Hartonomous.Infrastructure.Messaging.Events;

/// <summary>
/// Event published when an embedding is generated.
/// </summary>
public class EmbeddingGeneratedEvent : IntegrationEvent
{
    public required long EmbeddingId { get; init; }
    public required long AtomId { get; init; }
    public required string SourceType { get; init; }
    public int VectorDimensions { get; init; } = 768;
}

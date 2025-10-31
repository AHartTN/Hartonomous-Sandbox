namespace Hartonomous.Core.Entities;

/// <summary>
/// Stores individual components of an embedding to support dynamic dimensionality without vector padding.
/// </summary>
public class AtomEmbeddingComponent
{
    public long AtomEmbeddingComponentId { get; set; }

    public long AtomEmbeddingId { get; set; }

    public int ComponentIndex { get; set; }

    public float ComponentValue { get; set; }

    public AtomEmbedding AtomEmbedding { get; set; } = null!;
}

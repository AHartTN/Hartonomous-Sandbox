namespace Hartonomous.Core.Entities;

/// <summary>
/// Stores individual components of an embedding to support dynamic dimensionality without vector padding.
/// Enables storage of embeddings with arbitrary dimensions exceeding SQL Server VECTOR type limits (1998 floats).
/// Components can be selectively materialized for sparse or high-dimensional embeddings.
/// </summary>
public class AtomEmbeddingComponent
{
    /// <summary>
    /// Gets or sets the unique identifier for the embedding component.
    /// </summary>
    public long AtomEmbeddingComponentId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the parent atom embedding.
    /// </summary>
    public long AtomEmbeddingId { get; set; }

    /// <summary>
    /// Gets or sets the zero-based index of this component within the full embedding vector.
    /// </summary>
    public int ComponentIndex { get; set; }

    /// <summary>
    /// Gets or sets the float value of this embedding component.
    /// </summary>
    public float ComponentValue { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent atom embedding.
    /// </summary>
    public AtomEmbedding AtomEmbedding { get; set; } = null!;
}

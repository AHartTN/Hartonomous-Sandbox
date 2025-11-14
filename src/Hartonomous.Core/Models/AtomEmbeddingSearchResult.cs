using Hartonomous.Data.Entities;

namespace Hartonomous.Core.Models;

/// <summary>
/// Result payload for hybrid search queries over atom embeddings.
/// </summary>
public sealed class AtomEmbeddingSearchResult
{
    public required AtomEmbedding Embedding { get; init; }

    public double CosineDistance { get; init; }

    public double SpatialDistance { get; init; }
}

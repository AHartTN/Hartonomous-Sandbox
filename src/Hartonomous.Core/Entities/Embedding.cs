using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a vector embedding with dual representation for hybrid search.
/// Stores full-resolution VECTOR for exact similarity and 3D spatial projection for fast approximate search.
/// </summary>
public class Embedding
{
    /// <summary>
    /// Gets or sets the unique identifier for the embedding.
    /// </summary>
    public long EmbeddingId { get; set; }

    /// <summary>
    /// Gets or sets the source text that was embedded.
    /// </summary>
    public string? SourceText { get; set; }

    /// <summary>
    /// Gets or sets the type of source (e.g., 'token', 'sentence', 'document', 'image_patch').
    /// </summary>
    public required string SourceType { get; set; }

    /// <summary>
    /// Gets or sets the full-resolution embedding vector (mapped to SQL Server 2025 VECTOR type).
    /// Used for exact similarity searches via VECTOR_DISTANCE function.
    /// </summary>
    public SqlVector<float>? EmbeddingFull { get; set; }

    /// <summary>
    /// Gets or sets the name of the embedding model used (e.g., 'all-MiniLM-L6-v2', 'BERT-base').
    /// </summary>
    public string? EmbeddingModel { get; set; }

    /// <summary>
    /// Gets or sets the X coordinate of the 3D spatial projection.
    /// </summary>
    public double? SpatialProjX { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate of the 3D spatial projection.
    /// </summary>
    public double? SpatialProjY { get; set; }

    /// <summary>
    /// Gets or sets the Z coordinate of the 3D spatial projection.
    /// </summary>
    public double? SpatialProjZ { get; set; }

    /// <summary>
    /// Gets or sets the computed spatial geometry for spatial indexing (NTS Point).
    /// </summary>
    public Point? SpatialGeometry { get; set; }

    /// <summary>
    /// Gets or sets the coarse spatial geometry for hierarchical indexing (NTS Point).
    /// </summary>
    public Point? SpatialCoarse { get; set; }

    /// <summary>
    /// Gets or sets the dimensionality of the embedding vector.
    /// </summary>
    public int Dimension { get; set; } = 768;

    /// <summary>
    /// Gets or sets the timestamp when the embedding was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the number of times this embedding has been accessed.
    /// </summary>
    public int AccessCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the timestamp of the last access to this embedding.
    /// </summary>
    public DateTime? LastAccessed { get; set; }

    /// <summary>
    /// Gets or sets the SHA256 content hash for deduplication (Phase 2 addition).
    /// </summary>
    public string? ContentHash { get; set; }
}

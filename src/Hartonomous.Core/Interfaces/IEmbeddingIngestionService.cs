using Hartonomous.Core.Entities;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Service interface for embedding ingestion with deduplication.
/// Implements DI pattern for flexible, testable services.
/// </summary>
public interface IEmbeddingIngestionService
{
    /// <summary>
    /// Ingest a single embedding with deduplication checks.
    /// Checks content hash (SHA256) and semantic similarity (cosine distance).
    /// </summary>
    /// <param name="sourceText">Original text that was embedded</param>
    /// <param name="sourceType">Type of source (e.g., 'sentence', 'document', 'image_patch')</param>
    /// <param name="embeddingFull">Full-resolution embedding vector</param>
    /// <param name="spatial3D">Optional pre-computed 3D spatial projection. If null, will be computed.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ingestion result indicating whether embedding was new or duplicate</returns>
    Task<EmbeddingIngestionResult> IngestEmbeddingAsync(
        string sourceText,
        string sourceType,
        float[] embeddingFull,
        float[]? spatial3D = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingest multiple embeddings in a batch for efficiency.
    /// Uses EF Core AddRangeAsync for optimized bulk inserts.
    /// </summary>
    /// <param name="batch">Collection of embeddings to ingest</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of ingestion results</returns>
    Task<IEnumerable<EmbeddingIngestionResult>> IngestBatchAsync(
        IEnumerable<(string sourceText, string sourceType, float[] embedding)> batch,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of embedding ingestion operation.
/// </summary>
public class EmbeddingIngestionResult
{
    /// <summary>
    /// Gets or sets the atom embedding identifier created or reused during ingestion.
    /// </summary>
    public long? AtomEmbeddingId { get; set; }

    /// <summary>
    /// Backward compatible alias for <see cref="AtomEmbeddingId"/>.
    /// </summary>
    public long? EmbeddingId
    {
        get => AtomEmbeddingId;
        set => AtomEmbeddingId = value;
    }

    /// <summary>
    /// Gets or sets whether this was a duplicate (true) or new embedding (false).
    /// </summary>
    public bool WasDuplicate { get; set; }

    /// <summary>
    /// Gets or sets the reason for duplication if WasDuplicate is true.
    /// </summary>
    public string? DuplicateReason { get; set; }

    /// <summary>
    /// Gets or sets the associated atom identifier.
    /// </summary>
    public long AtomId { get; set; }

    /// <summary>
    /// Gets or sets the semantic similarity (cosine) when deduplication matched an existing embedding.
    /// </summary>
    public double? SemanticSimilarity { get; set; }
}

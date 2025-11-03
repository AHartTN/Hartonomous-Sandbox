namespace Hartonomous.Api.DTOs;

/// <summary>
/// Response model for embedding creation with deduplication information.
/// </summary>
public record EmbeddingResponse(
    /// <summary>
    /// Unique identifier for the atom.
    /// </summary>
    long AtomId,

    /// <summary>
    /// Unique identifier for the embedding (null if duplicate).
    /// </summary>
    long? EmbeddingId,

    /// <summary>
    /// Indicates whether this was a duplicate atom (content-addressable hash match).
    /// </summary>
    bool WasDuplicate,

    /// <summary>
    /// Reason for deduplication (e.g., "exact_hash_match", "semantic_similarity").
    /// </summary>
    string? DuplicateReason,

    /// <summary>
    /// Semantic similarity score if duplicate was found via similarity check.
    /// </summary>
    double? SemanticSimilarity
);

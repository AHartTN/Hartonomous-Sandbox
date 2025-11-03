namespace Hartonomous.Api.DTOs;

/// <summary>
/// Response model for search operations containing ranked results.
/// </summary>
public record SearchResponse(
    /// <summary>
    /// List of search results ordered by relevance.
    /// </summary>
    List<SearchResultItem> Results,

    /// <summary>
    /// Total number of results found.
    /// </summary>
    int TotalCount,

    /// <summary>
    /// Query execution time in milliseconds.
    /// </summary>
    double QueryTimeMs
);

/// <summary>
/// Individual search result item with relevance scores.
/// </summary>
public record SearchResultItem(
    /// <summary>
    /// Unique identifier for the atom.
    /// </summary>
    long AtomId,

    /// <summary>
    /// Unique identifier for the embedding.
    /// </summary>
    long EmbeddingId,

    /// <summary>
    /// Canonical text representation of the atom (if available).
    /// </summary>
    string? CanonicalText,

    /// <summary>
    /// Modality of the atom (text, image, audio, video).
    /// </summary>
    string Modality,

    /// <summary>
    /// Cosine similarity score (0-1, higher is better).
    /// </summary>
    double CosineSimilarity,

    /// <summary>
    /// Spatial distance in 3D projection space (lower is better).
    /// </summary>
    double? SpatialDistance
);

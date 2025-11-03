namespace Hartonomous.Api.DTOs;

/// <summary>
/// Request model for semantic and hybrid search operations.
/// </summary>
public record SearchRequest(
    /// <summary>
    /// Query vector for similarity search (must match embedding dimension).
    /// </summary>
    float[] QueryVector,

    /// <summary>
    /// Number of top results to return.
    /// </summary>
    int TopK = 10,

    /// <summary>
    /// Number of candidate results for spatial filtering (hybrid search only).
    /// </summary>
    int CandidateCount = 100,

    /// <summary>
    /// Optional topic filter to narrow search scope.
    /// </summary>
    string? TopicFilter = null,

    /// <summary>
    /// Optional minimum sentiment score filter.
    /// </summary>
    float? MinSentiment = null,

    /// <summary>
    /// Optional maximum age in days for temporal filtering.
    /// </summary>
    int? MaxAge = null
);

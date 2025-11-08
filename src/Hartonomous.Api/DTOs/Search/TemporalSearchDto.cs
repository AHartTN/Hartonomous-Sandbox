using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Search;

/// <summary>
/// Request for time-based temporal search.
/// </summary>
public class TemporalSearchRequest
{
    /// <summary>
    /// Query vector for semantic search.
    /// </summary>
    [Required]
    public required float[] QueryVector { get; set; }

    /// <summary>
    /// Start of the time range (UTC).
    /// </summary>
    [Required]
    public DateTime StartTimeUtc { get; set; }

    /// <summary>
    /// End of the time range (UTC).
    /// </summary>
    [Required]
    public DateTime EndTimeUtc { get; set; }

    /// <summary>
    /// Temporal search mode: "range" (between start/end), "point_in_time" (as of specific time), "changes" (delta between two times).
    /// </summary>
    [Required]
    public required string Mode { get; set; } = "range";

    /// <summary>
    /// Maximum number of results to return.
    /// </summary>
    [Range(1, 1000)]
    public int TopK { get; set; } = 10;

    /// <summary>
    /// Optional modality filter.
    /// </summary>
    public string? Modality { get; set; }

    /// <summary>
    /// Optional embedding type filter.
    /// </summary>
    public string? EmbeddingType { get; set; }

    /// <summary>
    /// Optional model ID filter.
    /// </summary>
    public int? ModelId { get; set; }
}

/// <summary>
/// Response for temporal search.
/// </summary>
public class TemporalSearchResponse
{
    /// <summary>
    /// Search results ordered by temporal relevance and similarity.
    /// </summary>
    public required List<TemporalSearchResult> Results { get; set; }

    /// <summary>
    /// Total number of results in the time range.
    /// </summary>
    public int TotalInRange { get; set; }

    /// <summary>
    /// Time range queried.
    /// </summary>
    public required string TimeRange { get; set; }

    /// <summary>
    /// Search mode used.
    /// </summary>
    public required string Mode { get; set; }
}

/// <summary>
/// Individual temporal search result.
/// </summary>
public class TemporalSearchResult
{
    /// <summary>
    /// AtomEmbedding identifier.
    /// </summary>
    public long AtomEmbeddingId { get; set; }

    /// <summary>
    /// Atom identifier.
    /// </summary>
    public long AtomId { get; set; }

    /// <summary>
    /// Content modality.
    /// </summary>
    public string? Modality { get; set; }

    /// <summary>
    /// Content subtype.
    /// </summary>
    public string? Subtype { get; set; }

    /// <summary>
    /// Source URI.
    /// </summary>
    public string? SourceUri { get; set; }

    /// <summary>
    /// Source type.
    /// </summary>
    public string? SourceType { get; set; }

    /// <summary>
    /// Vector similarity score.
    /// </summary>
    public double Similarity { get; set; }

    /// <summary>
    /// Creation timestamp (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Temporal distance from query midpoint in hours.
    /// </summary>
    public double TemporalDistanceHours { get; set; }
}

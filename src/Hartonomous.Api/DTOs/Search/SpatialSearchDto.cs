using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Search;

/// <summary>
/// Request for geography-based spatial search.
/// </summary>
public class SpatialSearchRequest
{
    /// <summary>
    /// Latitude of the query point (WGS84).
    /// </summary>
    [Range(-90, 90)]
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude of the query point (WGS84).
    /// </summary>
    [Range(-180, 180)]
    public double Longitude { get; set; }

    /// <summary>
    /// Search radius in meters. Default: 10,000 (10 km).
    /// </summary>
    [Range(1, 1000000)]
    public double RadiusMeters { get; set; } = 10000;

    /// <summary>
    /// Maximum number of results to return.
    /// </summary>
    [Range(1, 1000)]
    public int TopK { get; set; } = 10;

    /// <summary>
    /// Optional modality filter (e.g., "text", "image", "audio").
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
/// Response for spatial search.
/// </summary>
public class SpatialSearchResponse
{
    /// <summary>
    /// Search results ordered by spatial distance.
    /// </summary>
    public required List<SpatialSearchResult> Results { get; set; }

    /// <summary>
    /// Total number of results within the radius.
    /// </summary>
    public int TotalWithinRadius { get; set; }

    /// <summary>
    /// Query point (lat, long).
    /// </summary>
    public required string QueryPoint { get; set; }

    /// <summary>
    /// Search radius in meters.
    /// </summary>
    public double RadiusMeters { get; set; }
}

/// <summary>
/// Individual spatial search result.
/// </summary>
public class SpatialSearchResult
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
    /// Distance from query point in meters.
    /// </summary>
    public double DistanceMeters { get; set; }

    /// <summary>
    /// Spatial coordinates (lat, long).
    /// </summary>
    public string? Coordinates { get; set; }

    /// <summary>
    /// Vector similarity score (if available).
    /// </summary>
    public double? Similarity { get; set; }
}

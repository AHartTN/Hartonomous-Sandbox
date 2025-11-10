using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Search
{
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
}

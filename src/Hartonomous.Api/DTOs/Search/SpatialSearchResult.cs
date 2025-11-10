namespace Hartonomous.Api.DTOs.Search
{
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
}

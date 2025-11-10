using System;

namespace Hartonomous.Api.DTOs.Search
{
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
}

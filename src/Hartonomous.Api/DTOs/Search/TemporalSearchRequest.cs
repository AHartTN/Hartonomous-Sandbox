using System;
using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Search
{
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
}

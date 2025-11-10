using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Search
{
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
        public TemporalSearchMode Mode { get; set; }
    }
}

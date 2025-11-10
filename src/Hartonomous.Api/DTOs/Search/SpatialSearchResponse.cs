using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Search
{
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
}

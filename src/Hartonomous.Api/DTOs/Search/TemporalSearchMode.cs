namespace Hartonomous.Api.DTOs.Search
{
    /// <summary>
    /// Temporal search mode.
    /// </summary>
    public enum TemporalSearchMode
    {
        /// <summary>
        /// Search within a time range.
        /// </summary>
        Range,

        /// <summary>
        /// Point-in-time search (as of specific time).
        /// </summary>
        PointInTime,

        /// <summary>
        /// Changes between two times.
        /// </summary>
        Changes
    }
}

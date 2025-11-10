namespace Hartonomous.Api.DTOs.Search
{
    /// <summary>
    /// Individual search suggestion.
    /// </summary>
    public class Suggestion
    {
        /// <summary>
        /// Suggested query text.
        /// </summary>
        public required string Text { get; set; }

        /// <summary>
        /// Suggestion score/relevance (higher is better).
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Category/type of suggestion.
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Number of times this query has been used (if available).
        /// </summary>
        public int? UsageCount { get; set; }

        /// <summary>
        /// Whether this is a trending suggestion.
        /// </summary>
        public bool IsTrending { get; set; }
    }
}

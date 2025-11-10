using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Search
{
    /// <summary>
    /// Response containing search suggestions.
    /// </summary>
    public class SuggestionsResponse
    {
        /// <summary>
        /// List of suggested queries.
        /// </summary>
        public required List<Suggestion> Suggestions { get; set; }

        /// <summary>
        /// Query prefix used.
        /// </summary>
        public required string QueryPrefix { get; set; }

        /// <summary>
        /// Number of suggestions returned.
        /// </summary>
        public int Count { get; set; }
    }
}

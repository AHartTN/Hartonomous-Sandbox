using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Search
{
    /// <summary>
    /// Request for search query suggestions/autocomplete.
    /// </summary>
    public class SuggestionsRequest
    {
        /// <summary>
        /// Partial query text for autocomplete.
        /// </summary>
        [Required]
        [MinLength(1)]
        [MaxLength(500)]
        public required string QueryPrefix { get; set; }

        /// <summary>
        /// Maximum number of suggestions to return.
        /// </summary>
        [Range(1, 50)]
        public int MaxSuggestions { get; set; } = 10;

        /// <summary>
        /// Optional modality filter to scope suggestions.
        /// </summary>
        public string? Modality { get; set; }

        /// <summary>
        /// Optional source type filter.
        /// </summary>
        public string? SourceType { get; set; }

        /// <summary>
        /// Whether to include popular/trending suggestions.
        /// </summary>
        public bool IncludeTrending { get; set; } = true;
    }
}

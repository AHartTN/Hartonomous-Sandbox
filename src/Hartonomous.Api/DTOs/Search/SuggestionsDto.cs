using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Search;

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

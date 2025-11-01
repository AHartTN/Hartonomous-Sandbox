namespace Hartonomous.Core.ValueObjects;

/// <summary>
/// Result from embedding search operations (semantic, spatial, hybrid).
/// </summary>
public sealed class EmbeddingSearchResult
{
    public long EmbeddingId { get; init; }
    public string SourceText { get; init; } = string.Empty;
    public string SourceType { get; init; } = string.Empty;
    public float SimilarityScore { get; init; }
    public float Distance { get; init; }
    public DateTime CreatedTimestamp { get; init; }

    // Optional metadata
    public string? Topic { get; init; }
    public float? SentimentScore { get; init; }
    public int? ReferenceCount { get; init; }
}

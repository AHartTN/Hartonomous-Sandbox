namespace Hartonomous.Core.ValueObjects;

/// <summary>
/// Semantic features extracted from embeddings via clustering and spatial analysis.
/// </summary>
public sealed class SemanticFeatures
{
    public IReadOnlyList<string> Topics { get; init; } = Array.Empty<string>();
    public float SentimentScore { get; init; }
    public IReadOnlyList<string> Entities { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();
    public float TemporalRelevance { get; init; }
    public Dictionary<string, float> FeatureScores { get; init; } = new();
}

namespace Hartonomous.Core.ValueObjects;

/// <summary>
/// Result from text generation via spatial search.
/// </summary>
public sealed class GenerationResult
{
    public string GeneratedText { get; init; } = string.Empty;
    public IReadOnlyList<int> TokenIds { get; init; } = Array.Empty<int>();
    public IReadOnlyList<float> TokenConfidences { get; init; } = Array.Empty<float>();
    public int TokenCount { get; init; }
    public float AverageConfidence { get; init; }
    public long InferenceId { get; init; }
}

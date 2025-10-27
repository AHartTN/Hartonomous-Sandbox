namespace Hartonomous.Core.ValueObjects;

/// <summary>
/// Result from ensemble inference combining multiple models.
/// </summary>
public sealed class EnsembleInferenceResult
{
    public long InferenceId { get; init; }
    public string OutputData { get; init; } = string.Empty;
    public float ConfidenceScore { get; init; }
    public IReadOnlyList<ModelContribution> ModelContributions { get; init; } = Array.Empty<ModelContribution>();
    public DateTime CompletedTimestamp { get; init; }
}

/// <summary>
/// Contribution of a single model to ensemble result.
/// </summary>
public sealed class ModelContribution
{
    public int ModelId { get; init; }
    public string ModelName { get; init; } = string.Empty;
    public string IndividualOutput { get; init; } = string.Empty;
    public float Weight { get; init; }
    public float ConfidenceScore { get; init; }
}

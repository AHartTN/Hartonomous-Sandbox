using Hartonomous.Data.Entities.Entities;

namespace Hartonomous.Core.Interfaces;

public interface ISpatialInferenceService
{
    Task<IReadOnlyList<SpatialAttentionResult>> SpatialAttentionAsync(long queryTokenId, int contextSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SpatialNextTokenPrediction>> PredictNextTokenAsync(IEnumerable<long> contextTokenIds, double temperature, int topK, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MultiResolutionSearchResult>> MultiResolutionSearchAsync(double queryX, double queryY, double queryZ, int coarseCandidates, int fineCandidates, int topK, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CognitiveActivationResult>> CognitiveActivationAsync(float[] queryVector, double activationThreshold, int maxActivated, CancellationToken cancellationToken = default);
    Task<string> GenerateTextSpatialAsync(string prompt, int maxTokens, double temperature, CancellationToken cancellationToken = default);
}

public sealed record SpatialAttentionResult(long TokenId, string TokenText, double AttentionWeight, double SpatialDistance, string ResolutionLevel);

public sealed record SpatialNextTokenPrediction(long TokenId, string TokenText, double ProbabilityScore, double SpatialDistance);

public sealed record MultiResolutionSearchResult(
    long AtomEmbeddingId,
    long AtomId,
    string Modality,
    string? Subtype,
    string? SourceType,
    string? SourceUri,
    string? CanonicalText,
    string EmbeddingType,
    int? ModelId,
    double SpatialDistance,
    double CoarseDistance);

public sealed record CognitiveActivationResult(
    long AtomEmbeddingId,
    long AtomId,
    double ActivationStrength,
    string ActivationLevel,
    string? Modality,
    string? Subtype,
    string? SourceType,
    string? CanonicalText);

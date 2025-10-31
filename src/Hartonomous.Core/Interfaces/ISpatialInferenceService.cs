using Hartonomous.Core.Entities;

namespace Hartonomous.Core.Interfaces;

public interface ISpatialInferenceService
{
    Task<IReadOnlyList<(long TokenId, string Token, double AttentionWeight)>> SpatialAttentionAsync(long queryTokenId, int contextSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(long TokenId, string Token, double Probability)>> PredictNextTokenAsync(IEnumerable<long> contextTokenIds, double temperature, int topK, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Embedding>> MultiResolutionSearchAsync(double queryX, double queryY, double queryZ, int coarseCandidates, int fineCandidates, int topK, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(Embedding Embedding, double ActivationStrength, string Level)>> CognitiveActivationAsync(float[] queryVector, double activationThreshold, int maxActivated, CancellationToken cancellationToken = default);
    Task<string> GenerateTextSpatialAsync(string prompt, int maxTokens, double temperature, CancellationToken cancellationToken = default);
}

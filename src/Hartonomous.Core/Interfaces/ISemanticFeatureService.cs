using Hartonomous.Core.ValueObjects;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Provides semantic feature extraction and analysis capabilities.
/// Computes topic distributions, sentiment scores, and entity/keyword extraction from embeddings.
/// </summary>
public interface ISemanticFeatureService
{
    /// <summary>
    /// Computes semantic features for the supplied embeddings by calling stored procedures.
    /// Features include topic scores, sentiment analysis, formality, complexity, and extracted entities/keywords.
    /// </summary>
    /// <param name="atomEmbeddingIds">Identifiers of embeddings requiring feature extraction.</param>
    /// <param name="cancellationToken">Token to cancel stored procedure execution.</param>
    /// <returns>Aggregated semantic features across all provided embeddings.</returns>
    Task<SemanticFeatures> ComputeSemanticFeaturesAsync(
        IReadOnlyList<long> atomEmbeddingIds,
        CancellationToken cancellationToken = default);
}

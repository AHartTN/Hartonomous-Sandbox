using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces.Semantic;

/// <summary>
/// Service for semantic analysis operations.
/// </summary>
public interface ISemanticService
{
    /// <summary>
    /// Computes semantic features for an embedding.
    /// Calls sp_ComputeSemanticFeatures stored procedure.
    /// </summary>
    Task ComputeFeaturesAsync(
        long atomEmbeddingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes semantic features for all embeddings.
    /// Calls sp_ComputeAllSemanticFeatures stored procedure.
    /// </summary>
    Task ComputeAllFeaturesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects duplicate atoms via cosine similarity of embeddings.
    /// Calls sp_DetectDuplicates stored procedure.
    /// </summary>
    Task<IEnumerable<DuplicateResult>> DetectDuplicatesAsync(
        float similarityThreshold = 0.95f,
        int batchSize = 1000,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes semantic similarity between two atoms.
    /// Calls sp_SemanticSimilarity stored procedure.
    /// </summary>
    Task<float> ComputeSimilarityAsync(
        long atom1Id,
        long atom2Id,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts metadata (word count, char count, language) for atoms.
    /// Calls sp_ExtractMetadata stored procedure.
    /// </summary>
    Task ExtractMetadataAsync(
        long atomId,
        int tenantId = 0,
        CancellationToken cancellationToken = default);
}

public record DuplicateResult(
    long AtomId1,
    long AtomId2,
    float Similarity);

using Hartonomous.Core.Shared;
using NetTopologySuite.Geometries;

namespace Hartonomous.Infrastructure.Repositories.EfCore;

/// <summary>
/// Interface for vector search operations using EF Core with pgvector
/// </summary>
public interface IVectorSearchRepository
{
    /// <summary>
    /// Performs spatial pre-filtering + exact k-NN search
    /// </summary>
    Task<IReadOnlyList<VectorSearchResult>> SpatialVectorSearchAsync(
        byte[] queryVector,
        Geometry? spatialCenter = null,
        double? radiusMeters = null,
        int topK = 10,
        int tenantId = 0,
        double minSimilarity = 0.0);

    /// <summary>
    /// Performs temporal-aware vector search
    /// </summary>
    Task<IReadOnlyList<VectorSearchResult>> TemporalVectorSearchAsync(
        byte[] queryVector,
        DateTime asOfDate,
        int topK = 10,
        int tenantId = 0);

    /// <summary>
    /// Performs hybrid search combining vector similarity and text search
    /// </summary>
    Task<IReadOnlyList<HybridSearchResult>> HybridSearchAsync(
        byte[] queryVector,
        string? keywords = null,
        Geometry? spatialRegion = null,
        int topK = 10,
        double vectorWeight = 0.5,
        double keywordWeight = 0.3,
        double spatialWeight = 0.2,
        int tenantId = 0);

    /// <summary>
    /// Performs ensemble search across multiple embedding models
    /// </summary>
    Task<IReadOnlyList<EnsembleSearchResult>> MultiModelEnsembleSearchAsync(
        byte[] queryVector1, byte[] queryVector2, byte[] queryVector3,
        int model1Id, int model2Id, int model3Id,
        double model1Weight = 0.4, double model2Weight = 0.35, double model3Weight = 0.25,
        int topK = 10, int tenantId = 0);
}

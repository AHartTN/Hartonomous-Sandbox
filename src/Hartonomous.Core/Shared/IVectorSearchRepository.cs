using Hartonomous.Core.Entities;
using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Shared;

/// <summary>
/// Repository interface for vector search operations
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
    /// Performs point-in-time semantic search using temporal tables
    /// </summary>
    Task<IReadOnlyList<VectorSearchResult>> TemporalVectorSearchAsync(
        byte[] queryVector,
        DateTime asOfDate,
        int topK = 10,
        int tenantId = 0);

    /// <summary>
    /// Performs hybrid search combining full-text, vector, and spatial ranking
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
    /// Performs ensemble search blending results from multiple models
    /// </summary>
    Task<IReadOnlyList<EnsembleSearchResult>> MultiModelEnsembleSearchAsync(
        byte[] queryVector1, byte[] queryVector2, byte[] queryVector3,
        int model1Id, int model2Id, int model3Id,
        double model1Weight = 0.4, double model2Weight = 0.35, double model3Weight = 0.25,
        int topK = 10, int tenantId = 0);
}

/// <summary>
/// Result of a vector search operation
/// </summary>
public class VectorSearchResult
{
    public long AtomId { get; set; }
    public double Similarity { get; set; }
    public double SpatialDistance { get; set; }
    public byte[]? ContentHash { get; set; }
    public string? ContentType { get; set; }
    public DateTime CreatedUtc { get; set; }
}

/// <summary>
/// Result of a hybrid search operation
/// </summary>
public class HybridSearchResult
{
    public long AtomId { get; set; }
    public double VectorScore { get; set; }
    public double KeywordScore { get; set; }
    public double SpatialScore { get; set; }
    public double CombinedScore { get; set; }
    public byte[]? ContentHash { get; set; }
    public string? ContentType { get; set; }
    public DateTime CreatedUtc { get; set; }
}

/// <summary>
/// Result of an ensemble search operation
/// </summary>
public class EnsembleSearchResult
{
    public long AtomId { get; set; }
    public double Model1Score { get; set; }
    public double Model2Score { get; set; }
    public double Model3Score { get; set; }
    public double EnsembleScore { get; set; }
    public byte[]? ContentHash { get; set; }
    public string? ContentType { get; set; }
}
using Hartonomous.Core.Entities;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Repository for dimension-specific weight operations.
/// Supports index-only queries and DiskANN vector search.
/// </summary>
/// <typeparam name="TWeight">Weight entity type (Weight768, Weight1536, etc.)</typeparam>
public interface IWeightRepository<TWeight> where TWeight : WeightBase
{
    /// <summary>
    /// Gets weight by ID.
    /// </summary>
    Task<TWeight?> GetByIdAsync(long weightId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all weights for a model.
    /// Uses covering index for index-only scan.
    /// </summary>
    Task<IReadOnlyList<TWeight>> GetByModelAsync(
        int modelId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets weights for specific layers of a model.
    /// Optimized for student model extraction.
    /// </summary>
    Task<IReadOnlyList<TWeight>> GetByModelAndLayersAsync(
        int modelId,
        IEnumerable<int> layerIndices,
        float? minImportance = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets weights by importance score for creating student models.
    /// Returns top N most important weights across specified layers.
    /// </summary>
    Task<IReadOnlyList<TWeight>> GetTopImportantWeightsAsync(
        int modelId,
        int topN,
        IEnumerable<int>? layerIndices = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a single weight.
    /// </summary>
    Task<TWeight> AddAsync(TWeight weight, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk inserts weights using SqlBulkCopy for maximum performance.
    /// Essential for ingesting models with millions of parameters.
    /// </summary>
    Task BulkInsertAsync(IEnumerable<TWeight> weights, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates importance scores for weights (e.g., after pruning analysis).
    /// </summary>
    Task UpdateImportanceScoresAsync(
        IDictionary<long, float> weightScores,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds similar weights using VECTOR_DISTANCE (exact search).
    /// For small datasets (&lt;50K vectors). Use DiskANN search for larger datasets.
    /// </summary>
    Task<IReadOnlyList<(TWeight Weight, float Distance)>> FindSimilarWeightsExactAsync(
        string queryVectorJson,
        int topK,
        int? modelId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds similar weights using DiskANN approximate search.
    /// Requires VECTOR INDEX to be created on the table.
    /// For large datasets (>50K vectors).
    /// </summary>
    Task<IReadOnlyList<(TWeight Weight, float Distance)>> FindSimilarWeightsApproximateAsync(
        string queryVectorJson,
        int topK,
        int? modelId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets count of weights for a model.
    /// </summary>
    Task<int> GetCountByModelAsync(int modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if table has VECTOR INDEX created (table must be readonly).
    /// </summary>
    Task<bool> HasVectorIndexAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for managing ModelArchitecture catalog and routing to correct weight tables.
/// </summary>
public interface IModelArchitectureService
{
    /// <summary>
    /// Gets model architecture by ID.
    /// </summary>
    Task<ModelArchitecture?> GetByIdAsync(int modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets model architecture by name.
    /// </summary>
    Task<ModelArchitecture?> GetByNameAsync(string modelName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new model architecture.
    /// Automatically determines weights table based on dimension.
    /// </summary>
    Task<ModelArchitecture> RegisterModelAsync(
        string modelName,
        string modelType,
        int embeddingDimension,
        int layerCount,
        long? parameterCount = null,
        string? architectureConfig = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all models with a specific dimension.
    /// </summary>
    Task<IReadOnlyList<ModelArchitecture>> GetByDimensionAsync(
        int dimension,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the appropriate weight repository for a model.
    /// Routes to Weights_768, Weights_1536, Weights_1998, or Weights_3996.
    /// </summary>
    IWeightRepository<WeightBase> GetWeightRepositoryForModel(int modelId);

    /// <summary>
    /// Updates model metadata.
    /// </summary>
    Task UpdateAsync(ModelArchitecture model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a model (soft delete).
    /// </summary>
    Task DeactivateAsync(int modelId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for managing WeightCatalog and content-based deduplication.
/// </summary>
public interface IWeightCatalogService
{
    /// <summary>
    /// Adds weight to catalog with content hash.
    /// </summary>
    Task<WeightCatalog> AddToCatalogAsync(
        long weightId,
        int modelId,
        int layerIdx,
        string componentType,
        byte[] contentHash,
        float? importanceScore = null,
        string? positionMetadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds duplicate weights by content hash within same dimension.
    /// </summary>
    Task<IReadOnlyList<(byte[] Hash, int Count, IReadOnlyList<int> ModelIds)>> FindDuplicatesAsync(
        int? modelId = null,
        int minDuplicates = 2,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a weight with this hash already exists for this model.
    /// Returns existing weight_id if found, null otherwise.
    /// </summary>
    Task<long?> FindExistingWeightByHashAsync(
        int modelId,
        byte[] contentHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets catalog entries for a model.
    /// </summary>
    Task<IReadOnlyList<WeightCatalog>> GetByModelAsync(
        int modelId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates importance score in catalog.
    /// </summary>
    Task UpdateImportanceScoreAsync(
        long catalogId,
        float importanceScore,
        CancellationToken cancellationToken = default);
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Interfaces.Search;

/// <summary>
/// Service for all search operations including semantic, hybrid, fusion, and spatial searches.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Semantic search via text embedding similarity.
    /// Calls sp_SemanticSearch stored procedure.
    /// </summary>
    Task<IEnumerable<SearchResult>> SemanticSearchAsync(
        string queryText,
        int topK = 10,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hybrid search combining BM25 full-text and vector similarity.
    /// Calls sp_HybridSearch stored procedure.
    /// </summary>
    Task<IEnumerable<SearchResult>> HybridSearchAsync(
        string textQuery,
        byte[] queryVector,
        int topK = 10,
        float textWeight = 0.4f,
        float vectorWeight = 0.6f,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fusion search combining vector, keyword, and spatial scoring.
    /// Calls sp_FusionSearch stored procedure.
    /// </summary>
    Task<IEnumerable<SearchResult>> FusionSearchAsync(
        byte[] queryVector,
        string? keywords = null,
        Geometry? spatialRegion = null,
        int topK = 10,
        float vectorWeight = 0.5f,
        float keywordWeight = 0.3f,
        float spatialWeight = 0.2f,
        int? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exact vector similarity search with tenancy filtering.
    /// Calls sp_ExactVectorSearch stored procedure.
    /// </summary>
    Task<IEnumerable<SearchResult>> ExactVectorSearchAsync(
        byte[] queryVector,
        int topK = 10,
        int tenantId = 0,
        string distanceMetric = "cosine",
        string? embeddingType = null,
        int? modelId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Vector search with semantic filter predicates.
    /// Calls sp_SemanticFilteredSearch stored procedure.
    /// </summary>
    Task<IEnumerable<SearchResult>> FilteredSearchAsync(
        byte[] queryVector,
        string filtersJson,
        int topK = 10,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Vector search filtered by temporal range.
    /// Calls sp_TemporalVectorSearch stored procedure.
    /// </summary>
    Task<IEnumerable<SearchResult>> TemporalSearchAsync(
        byte[] queryVector,
        DateTime startDate,
        DateTime endDate,
        int topK = 10,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cross-modal search combining text and spatial queries.
    /// Calls sp_CrossModalQuery stored procedure.
    /// </summary>
    Task<IEnumerable<SearchResult>> CrossModalSearchAsync(
        string? textQuery = null,
        float? spatialX = null,
        float? spatialY = null,
        float? spatialZ = null,
        string? modalityFilter = null,
        int topK = 10,
        CancellationToken cancellationToken = default);
}

public record SearchResult(
    long AtomId,
    float Score,
    string? Modality,
    string? ContentPreview,
    DateTime? CreatedAt);

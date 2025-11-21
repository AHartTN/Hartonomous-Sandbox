using Hartonomous.Core.Interfaces.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// API endpoints for semantic, hybrid, and fusion search operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ISearchService searchService,
        ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Perform pure semantic vector search.
    /// </summary>
    /// <param name="request">Search request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results ordered by relevance</returns>
    [HttpPost("semantic")]
    [ProducesResponseType(typeof(IEnumerable<SearchResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<SearchResult>>> SemanticSearch(
        [FromBody] SemanticSearchRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("SemanticSearch API called: Query '{Query}', TopK {TopK}",
            request.Query, request.TopK);

        var results = await _searchService.SemanticSearchAsync(
            request.Query,
            request.TopK ?? 10,
            request.TenantId ?? 0,
            cancellationToken);

        return Ok(results);
    }

    /// <summary>
    /// Perform hybrid search combining BM25 keyword matching with vector similarity.
    /// </summary>
    /// <param name="request">Hybrid search request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Blended search results</returns>
    [HttpPost("hybrid")]
    [ProducesResponseType(typeof(IEnumerable<SearchResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<SearchResult>>> HybridSearch(
        [FromBody] HybridSearchRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("HybridSearch API called: Text '{Text}', TopK {TopK}",
            request.Text, request.TopK);

        var results = await _searchService.HybridSearchAsync(
            request.Text,
            request.Vector,
            request.TopK ?? 10,
            request.TextWeight ?? 0.3f,
            request.VectorWeight ?? 0.7f,
            request.TenantId ?? 0,
            cancellationToken);

        return Ok(results);
    }

    /// <summary>
    /// Perform fusion search combining vector, keyword, and spatial components.
    /// </summary>
    /// <param name="request">Fusion search request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Fused search results</returns>
    [HttpPost("fusion")]
    [ProducesResponseType(typeof(IEnumerable<SearchResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<SearchResult>>> FusionSearch(
        [FromBody] FusionSearchRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("FusionSearch API called: TopK {TopK}", request.TopK);

        // Convert spatial coordinates to Geometry if provided
        NetTopologySuite.Geometries.Geometry? spatialRegion = null;
        if (request.SpatialX.HasValue && request.SpatialY.HasValue && request.SpatialZ.HasValue)
        {
            spatialRegion = new NetTopologySuite.Geometries.Point(
                request.SpatialX.Value,
                request.SpatialY.Value,
                request.SpatialZ.Value);
        }

        // Parse weights array into individual weights (vectorWeight, keywordWeight, spatialWeight)
        float vectorWeight = 0.5f;
        float keywordWeight = 0.3f;
        float spatialWeight = 0.2f;

        if (request.Weights != null && request.Weights.Length >= 3)
        {
            vectorWeight = request.Weights[0];
            keywordWeight = request.Weights[1];
            spatialWeight = request.Weights[2];
        }

        var results = await _searchService.FusionSearchAsync(
            request.Vector,
            request.Keywords,
            spatialRegion,
            request.TopK ?? 10,
            vectorWeight,
            keywordWeight,
            spatialWeight,
            request.TenantId,
            cancellationToken);

        return Ok(results);
    }

    /// <summary>
    /// Perform exact brute-force vector search (no spatial optimization).
    /// </summary>
    /// <param name="request">Exact search request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exact search results</returns>
    [HttpPost("exact")]
    [ProducesResponseType(typeof(IEnumerable<SearchResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<SearchResult>>> ExactVectorSearch(
        [FromBody] ExactSearchRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("ExactVectorSearch API called: Metric {Metric}, TopK {TopK}",
            request.Metric, request.TopK);

        var results = await _searchService.ExactVectorSearchAsync(
            request.Vector,
            request.TopK ?? 10,
            request.TenantId ?? 0,
            request.Metric ?? "cosine",
            request.EmbeddingType ?? "semantic",
            request.ModelId,
            cancellationToken);

        return Ok(results);
    }

    /// <summary>
    /// Perform semantic search with metadata filters.
    /// </summary>
    /// <param name="request">Filtered search request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered search results</returns>
    [HttpPost("filtered")]
    [ProducesResponseType(typeof(IEnumerable<SearchResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<SearchResult>>> FilteredSearch(
        [FromBody] FilteredSearchRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("FilteredSearch API called: Filters {Filters}, TopK {TopK}",
            request.Filters, request.TopK);

        var results = await _searchService.FilteredSearchAsync(
            request.Vector,
            request.Filters,
            request.TopK ?? 10,
            request.TenantId ?? 0,
            cancellationToken);

        return Ok(results);
    }

    /// <summary>
    /// Perform time-bounded vector search.
    /// </summary>
    /// <param name="request">Temporal search request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Time-filtered search results</returns>
    [HttpPost("temporal")]
    [ProducesResponseType(typeof(IEnumerable<SearchResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<SearchResult>>> TemporalSearch(
        [FromBody] TemporalSearchRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("TemporalSearch API called: Range {Start} to {End}, TopK {TopK}",
            request.StartTime, request.EndTime, request.TopK);

        var results = await _searchService.TemporalSearchAsync(
            request.Vector,
            request.StartTime,
            request.EndTime,
            request.TopK ?? 10,
            request.TenantId ?? 0,
            cancellationToken);

        return Ok(results);
    }

    /// <summary>
    /// Perform cross-modal search (text to image, image to audio, etc.).
    /// </summary>
    /// <param name="request">Cross-modal search request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cross-modal search results</returns>
    [HttpPost("cross-modal")]
    [ProducesResponseType(typeof(IEnumerable<SearchResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<SearchResult>>> CrossModalSearch(
        [FromBody] CrossModalSearchRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("CrossModalSearch API called: Query '{Query}', Filter {Filter}, TopK {TopK}",
            request.TextQuery, request.ModalityFilter, request.TopK);

        var results = await _searchService.CrossModalSearchAsync(
            request.TextQuery,
            request.SpatialX,
            request.SpatialY,
            request.SpatialZ,
            request.ModalityFilter,
            request.TopK ?? 10,
            cancellationToken);

        return Ok(results);
    }
}

#region Request DTOs

public record SemanticSearchRequest(
    [Required] string Query,
    int? TopK = 10,
    int? TenantId = 0);

public record HybridSearchRequest(
    [Required] string Text,
    [Required] byte[] Vector,
    int? TopK = 10,
    float? TextWeight = 0.3f,
    float? VectorWeight = 0.7f,
    int? TenantId = 0);

public record FusionSearchRequest(
    [Required] byte[] Vector,
    string? Keywords = null,
    float? SpatialX = null,
    float? SpatialY = null,
    float? SpatialZ = null,
    int? TopK = 10,
    float[]? Weights = null,
    int? TenantId = 0);

public record ExactSearchRequest(
    [Required] byte[] Vector,
    int? TopK = 10,
    int? TenantId = 0,
    string? Metric = "cosine",
    string? EmbeddingType = "semantic",
    int? ModelId = null);

public record FilteredSearchRequest(
    [Required] byte[] Vector,
    [Required] string Filters,
    int? TopK = 10,
    int? TenantId = 0);

public record TemporalSearchRequest(
    [Required] byte[] Vector,
    [Required] DateTime StartTime,
    [Required] DateTime EndTime,
    int? TopK = 10,
    int? TenantId = 0);

public record CrossModalSearchRequest(
    [Required] string TextQuery,
    float? SpatialX = null,
    float? SpatialY = null,
    float? SpatialZ = null,
    string? ModalityFilter = null,
    int? TopK = 10);

#endregion

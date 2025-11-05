using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Hartonomous.Api.DTOs;
using Hartonomous.Api.DTOs.Search;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Models;
using Hartonomous.Core.Utilities;
using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Shared.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Hartonomous.Api.Controllers;

[Route("api/search")]
public sealed class SearchController : ApiControllerBase
{
    private readonly IInferenceService _inferenceService;
    private readonly IEmbeddingService _embeddingService;
    private readonly string _connectionString;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        IInferenceService inferenceService, 
        IEmbeddingService embeddingService,
        IConfiguration configuration,
        ILogger<SearchController> logger)
    {
        _inferenceService = inferenceService ?? throw new ArgumentNullException(nameof(inferenceService));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not configured");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SearchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SearchResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SearchResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SearchResponse>>> SearchAsync(
        [FromBody] SearchRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Failure<SearchResponse>(new[] { ValidationError("Request body is required.") }));
        }

        if (request.QueryVector is null || request.QueryVector.Length == 0)
        {
            return BadRequest(Failure<SearchResponse>(new[] { MissingField(nameof(request.QueryVector)) }));
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();
            IReadOnlyList<AtomEmbeddingSearchResult> results;

            var hasFilters = !string.IsNullOrWhiteSpace(request.TopicFilter)
                || request.MinSentiment.HasValue
                || request.MaxAge.HasValue;

            if (hasFilters)
            {
                results = await _inferenceService
                    .SemanticFilteredSearchAsync(
                        request.QueryVector,
                        Math.Max(1, request.TopK),
                        request.TopicFilter,
                        request.MinSentiment,
                        request.MaxAge,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                results = await _inferenceService
                    .HybridSearchAsync(
                        request.QueryVector,
                        Math.Max(1, request.TopK),
                        Math.Max(10, request.CandidateCount),
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            stopwatch.Stop();

            var mapped = results
                .Select(MapResult)
                .ToList();

            var response = new SearchResponse(
                mapped,
                mapped.Count,
                stopwatch.Elapsed.TotalMilliseconds);

            var metadata = new Dictionary<string, object?>
            {
                ["strategy"] = hasFilters ? "semantic-filtered" : "hybrid",
                ["requestedTopK"] = request.TopK,
                ["candidateCount"] = request.CandidateCount
            };

            return Ok(Success(response, metadata));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid search request received.");
            var error = ErrorDetailFactory.Validation(ex.Message);
            return BadRequest(Failure<SearchResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search execution failed.");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "An unexpected error occurred while executing the search.");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<SearchResponse>(new[] { error }));
        }
    }

    [HttpPost("cross-modal")]
    [ProducesResponseType(typeof(ApiResponse<DTOs.Search.CrossModalSearchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DTOs.Search.CrossModalSearchResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<DTOs.Search.CrossModalSearchResponse>>> CrossModalSearchAsync(
        [FromBody] DTOs.Search.CrossModalSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Failure<DTOs.Search.CrossModalSearchResponse>(new[] { ValidationError("Request body is required.") }));
        }

        if (string.IsNullOrWhiteSpace(request.QueryText) && (request.QueryEmbedding is null || request.QueryEmbedding.Length == 0))
        {
            return BadRequest(Failure<DTOs.Search.CrossModalSearchResponse>(new[] { ValidationError("Either QueryText or QueryEmbedding is required.") }));
        }

        if (request.TargetModalities is null || request.TargetModalities.Count == 0)
        {
            return BadRequest(Failure<DTOs.Search.CrossModalSearchResponse>(new[] { ValidationError("At least one TargetModality is required.") }));
        }

        try
        {
            float[] embedding;
            
            if (request.QueryEmbedding is not null && request.QueryEmbedding.Length > 0)
            {
                embedding = request.QueryEmbedding;
            }
            else
            {
                var queryText = request.QueryText ?? string.Empty;
                _logger.LogInformation("Generating embedding for cross-modal search: {Text}", queryText[..Math.Min(50, queryText.Length)]);
                embedding = await _embeddingService.EmbedTextAsync(queryText, cancellationToken).ConfigureAwait(false);
            }

            var results = new List<DTOs.Search.SearchResult>();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new SqlCommand("dbo.sp_ExactVectorSearch", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 60
            };

            var padded = VectorUtility.PadToSqlLength(embedding, out _);
            var vectorParam = command.Parameters.AddWithValue("@query_vector", padded);
            vectorParam.SqlDbType = SqlDbType.Udt;
            vectorParam.UdtTypeName = "VECTOR(1998)";

            command.Parameters.AddWithValue("@top_k", request.TopK * 2);
            command.Parameters.AddWithValue("@distance_metric", request.DistanceMetric ?? "cosine");
            command.Parameters.AddWithValue("@embedding_type", request.EmbeddingType ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ModelId", request.ModelId ?? (object)DBNull.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var modality = reader.IsDBNull(2) ? null : reader.GetString(2);
                
                if (modality is not null && request.TargetModalities.Contains(modality, StringComparer.OrdinalIgnoreCase))
                {
                    results.Add(new DTOs.Search.SearchResult
                    {
                        AtomEmbeddingId = reader.GetInt64(0),
                        AtomId = reader.GetInt64(1),
                        Modality = modality,
                        Subtype = reader.IsDBNull(3) ? null : reader.GetString(3),
                        SourceUri = reader.IsDBNull(4) ? null : reader.GetString(4),
                        SourceType = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Distance = reader.GetDouble(9),
                        Similarity = reader.GetDouble(10)
                    });
                }

                if (results.Count >= request.TopK)
                {
                    break;
                }
            }

            _logger.LogInformation("Cross-modal search returned {Count} results across modalities: {Modalities}", 
                results.Count, string.Join(", ", request.TargetModalities));

            var response = new DTOs.Search.CrossModalSearchResponse
            {
                Results = results,
                QueryModality = "text",
                TargetModalities = request.TargetModalities
            };

            var metadata = new Dictionary<string, object?>
            {
                ["resultCount"] = results.Count,
                ["targetModalities"] = string.Join(", ", request.TargetModalities)
            };

            return Ok(Success(response, metadata));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error during cross-modal search");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Cross-modal search failed", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<DTOs.Search.CrossModalSearchResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cross-modal search failed");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "An unexpected error occurred during cross-modal search.");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<DTOs.Search.CrossModalSearchResponse>(new[] { error }));
        }
    }

    private static SearchResultItem MapResult(AtomEmbeddingSearchResult result)
    {
        var embedding = result.Embedding;
        var atom = embedding.Atom;

        var similarity = 1d - result.CosineDistance;

        return new SearchResultItem(
            atom.AtomId,
            embedding.AtomEmbeddingId,
            atom.CanonicalText,
            atom.Modality,
            similarity,
            result.SpatialDistance == 0 ? null : result.SpatialDistance);
    }
}

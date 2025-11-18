using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Hartonomous.Api.DTOs;
using Hartonomous.Api.DTOs.Search;
using Hartonomous.Core.Enums;
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
        _connectionString = configuration.GetConnectionString("HartonomousDb") 
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
                QueryModality = Modality.Text.ToJsonString(),
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

    /// <summary>
    /// Geographic spatial search using lat/long and radius.
    /// </summary>
    /// <param name="request">Spatial search parameters (lat, long, radius, filters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results ordered by spatial distance.</returns>
    [HttpPost("spatial")]
    [ProducesResponseType(typeof(ApiResponse<DTOs.Search.SpatialSearchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DTOs.Search.SpatialSearchResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DTOs.Search.SpatialSearchResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DTOs.Search.SpatialSearchResponse>>> SpatialSearchAsync(
        [FromBody] DTOs.Search.SpatialSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Failure<DTOs.Search.SpatialSearchResponse>(new[] { ValidationError("Request body is required.") }));
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var results = new List<DTOs.Search.SpatialSearchResult>();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // Create geography point from lat/long (WGS84 SRID 4326)
            await using var command = new SqlCommand(@"
                DECLARE @queryPoint GEOGRAPHY = geography::Point(@lat, @long, 4326);
                DECLARE @radiusMeters FLOAT = @radius;

                SELECT TOP (@topK)
                    ae.AtomEmbeddingId,
                    ae.AtomId,
                    a.Modality,
                    a.Subtype,
                    a.SourceUri,
                    a.SourceType,
                    ae.SpatialGeometry.STDistance(@queryPoint) AS DistanceMeters,
                    ae.SpatialGeometry.Lat AS Lat,
                    ae.SpatialGeometry.Long AS Long,
                    CASE 
                        WHEN ae.EmbeddingVector IS NOT NULL THEN 1.0
                        ELSE NULL 
                    END AS Similarity
                FROM dbo.AtomEmbeddings ae
                INNER JOIN dbo.Atoms a ON a.AtomId = ae.AtomId
                WHERE ae.SpatialGeometry IS NOT NULL
                  AND ae.SpatialGeometry.STDistance(@queryPoint) <= @radiusMeters
                  AND (@modality IS NULL OR a.Modality = @modality)
                  AND (@embeddingType IS NULL OR ae.EmbeddingType = @embeddingType)
                  AND (@modelId IS NULL OR ae.ModelId = @modelId)
                ORDER BY ae.SpatialGeometry.STDistance(@queryPoint) ASC;
            ", connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 60
            };

            command.Parameters.AddWithValue("@lat", request.Latitude);
            command.Parameters.AddWithValue("@long", request.Longitude);
            command.Parameters.AddWithValue("@radius", request.RadiusMeters);
            command.Parameters.AddWithValue("@topK", request.TopK);
            command.Parameters.AddWithValue("@modality", request.Modality ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@embeddingType", request.EmbeddingType ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@modelId", request.ModelId ?? (object)DBNull.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                results.Add(new DTOs.Search.SpatialSearchResult
                {
                    AtomEmbeddingId = reader.GetInt64(0),
                    AtomId = reader.GetInt64(1),
                    Modality = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Subtype = reader.IsDBNull(3) ? null : reader.GetString(3),
                    SourceUri = reader.IsDBNull(4) ? null : reader.GetString(4),
                    SourceType = reader.IsDBNull(5) ? null : reader.GetString(5),
                    DistanceMeters = reader.GetDouble(6),
                    Coordinates = reader.IsDBNull(7) || reader.IsDBNull(8) 
                        ? null 
                        : $"{reader.GetDouble(7)}, {reader.GetDouble(8)}",
                    Similarity = reader.IsDBNull(9) ? null : reader.GetDouble(9)
                });
            }

            stopwatch.Stop();

            var response = new DTOs.Search.SpatialSearchResponse
            {
                Results = results,
                TotalWithinRadius = results.Count,
                QueryPoint = $"{request.Latitude}, {request.Longitude}",
                RadiusMeters = request.RadiusMeters
            };

            var metadata = new Dictionary<string, object?>
            {
                ["durationMs"] = stopwatch.Elapsed.TotalMilliseconds,
                ["resultCount"] = results.Count
            };

            _logger.LogInformation("Spatial search completed: {Count} results within {Radius}m of ({Lat}, {Long})", 
                results.Count, request.RadiusMeters, request.Latitude, request.Longitude);

            return Ok(Success(response, metadata));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error during spatial search");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Spatial search failed", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<DTOs.Search.SpatialSearchResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Spatial search failed");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "An unexpected error occurred during spatial search.");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<DTOs.Search.SpatialSearchResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Temporal search filtering by time range with semantic similarity.
    /// </summary>
    /// <param name="request">Temporal search parameters (vector, time range, mode, filters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results ordered by temporal relevance and similarity.</returns>
    [HttpPost("temporal")]
    [ProducesResponseType(typeof(ApiResponse<DTOs.Search.TemporalSearchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DTOs.Search.TemporalSearchResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DTOs.Search.TemporalSearchResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DTOs.Search.TemporalSearchResponse>>> TemporalSearchAsync(
        [FromBody] DTOs.Search.TemporalSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Failure<DTOs.Search.TemporalSearchResponse>(new[] { ValidationError("Request body is required.") }));
        }

        if (request.QueryVector is null || request.QueryVector.Length == 0)
        {
            return BadRequest(Failure<DTOs.Search.TemporalSearchResponse>(new[] { MissingField(nameof(request.QueryVector)) }));
        }

        if (request.StartTimeUtc >= request.EndTimeUtc)
        {
            return BadRequest(Failure<DTOs.Search.TemporalSearchResponse>(new[] { 
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidFieldValue, "StartTimeUtc must be before EndTimeUtc.")
            }));
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // Use database-centric stored procedure sp_TemporalVectorSearch
            await using var command = new SqlCommand("dbo.sp_TemporalVectorSearch", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 60
            };

            var padded = Core.Utilities.VectorUtility.PadToSqlLength(request.QueryVector, out var actualDim);
            var vectorParam = command.Parameters.AddWithValue("@QueryVector", padded);
            vectorParam.SqlDbType = SqlDbType.Udt;
            vectorParam.UdtTypeName = "VECTOR(1998)";

            command.Parameters.AddWithValue("@TopK", request.TopK);
            command.Parameters.AddWithValue("@StartTime", request.StartTimeUtc);
            command.Parameters.AddWithValue("@EndTime", request.EndTimeUtc);
            command.Parameters.AddWithValue("@Modality", request.Modality ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@EmbeddingType", request.EmbeddingType ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ModelId", request.ModelId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Dimension", actualDim);

            // Stored procedure returns JSON - parse directly
            var jsonResult = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            var results = jsonResult != null && jsonResult != DBNull.Value
                ? System.Text.Json.JsonSerializer.Deserialize<List<DTOs.Search.TemporalSearchResult>>(jsonResult.ToString()!)
                : new List<DTOs.Search.TemporalSearchResult>();

            stopwatch.Stop();

            var response = new DTOs.Search.TemporalSearchResponse
            {
                Results = results,
                TotalInRange = results.Count,
                TimeRange = $"{request.StartTimeUtc:yyyy-MM-dd HH:mm:ss} to {request.EndTimeUtc:yyyy-MM-dd HH:mm:ss}",
                Mode = request.Mode
            };

            var metadata = new Dictionary<string, object?>
            {
                ["durationMs"] = stopwatch.Elapsed.TotalMilliseconds,
                ["resultCount"] = results.Count,
                ["mode"] = request.Mode
            };

            _logger.LogInformation("Temporal search completed: {Count} results in range {Start} to {End}, mode {Mode}", 
                results.Count, request.StartTimeUtc, request.EndTimeUtc, request.Mode);

            return Ok(Success(response, metadata));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error during temporal search");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Temporal search failed", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<DTOs.Search.TemporalSearchResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Temporal search failed");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "An unexpected error occurred during temporal search.");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<DTOs.Search.TemporalSearchResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Get search query suggestions/autocomplete based on prefix.
    /// </summary>
    /// <param name="request">Suggestion parameters (query prefix, filters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of suggested queries ordered by relevance.</returns>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(ApiResponse<DTOs.Search.SuggestionsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DTOs.Search.SuggestionsResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DTOs.Search.SuggestionsResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DTOs.Search.SuggestionsResponse>>> GetSuggestionsAsync(
        [FromQuery] string queryPrefix,
        [FromQuery] int maxSuggestions = 10,
        [FromQuery] string? modality = null,
        [FromQuery] string? sourceType = null,
        [FromQuery] bool includeTrending = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(queryPrefix))
        {
            return BadRequest(Failure<DTOs.Search.SuggestionsResponse>(new[] { 
                MissingField("queryPrefix")
            }));
        }

        if (queryPrefix.Length > 500)
        {
            return BadRequest(Failure<DTOs.Search.SuggestionsResponse>(new[] { 
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidFieldValue, "queryPrefix must be 500 characters or less.")
            }));
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var suggestions = new List<DTOs.Search.Suggestion>();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // Generate suggestions from CanonicalText (simple prefix match)
            // In production: use FTS index, query logs, or dedicated suggestions table
            await using var command = new SqlCommand(@"
                SELECT TOP (@maxSuggestions)
                    a.CanonicalText AS SuggestionText,
                    COUNT(*) AS UsageCount,
                    MAX(a.CreatedAtUtc) AS LastUsed,
                    a.Modality AS Category,
                    CASE 
                        WHEN MAX(a.CreatedAtUtc) >= DATEADD(DAY, -7, SYSUTCDATETIME()) THEN 1 
                        ELSE 0 
                    END AS IsTrending
                FROM dbo.Atoms a
                WHERE a.CanonicalText LIKE @prefix + '%'
                  AND LEN(a.CanonicalText) <= 200
                  AND (@modality IS NULL OR a.Modality = @modality)
                  AND (@sourceType IS NULL OR a.SourceType = @sourceType)
                GROUP BY a.CanonicalText, a.Modality
                ORDER BY COUNT(*) DESC, MAX(a.CreatedAtUtc) DESC;
            ", connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 30
            };

            command.Parameters.AddWithValue("@prefix", queryPrefix);
            command.Parameters.AddWithValue("@maxSuggestions", Math.Max(1, Math.Min(maxSuggestions, 50)));
            command.Parameters.AddWithValue("@modality", modality ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@sourceType", sourceType ?? (object)DBNull.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var text = reader.GetString(0);
                var usageCount = reader.GetInt32(1);
                var isTrending = reader.GetInt32(4) == 1;

                if (!includeTrending && isTrending)
                {
                    continue;
                }

                suggestions.Add(new DTOs.Search.Suggestion
                {
                    Text = text,
                    Score = usageCount, // Simple scoring: usage count
                    Category = reader.IsDBNull(3) ? null : reader.GetString(3),
                    UsageCount = usageCount,
                    IsTrending = isTrending
                });
            }

            stopwatch.Stop();

            var response = new DTOs.Search.SuggestionsResponse
            {
                Suggestions = suggestions,
                QueryPrefix = queryPrefix,
                Count = suggestions.Count
            };

            var metadata = new Dictionary<string, object?>
            {
                ["durationMs"] = stopwatch.Elapsed.TotalMilliseconds,
                ["count"] = suggestions.Count
            };

            _logger.LogInformation("Suggestions query completed: {Count} suggestions for prefix '{Prefix}'", 
                suggestions.Count, queryPrefix.Length > 50 ? queryPrefix.Substring(0, 50) + "..." : queryPrefix);

            return Ok(Success(response, metadata));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error during suggestions query");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Suggestions query failed", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<DTOs.Search.SuggestionsResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Suggestions query failed");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "An unexpected error occurred during suggestions query.");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<DTOs.Search.SuggestionsResponse>(new[] { error }));
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

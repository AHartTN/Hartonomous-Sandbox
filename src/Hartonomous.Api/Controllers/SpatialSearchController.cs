using Hartonomous.Api.DTOs.Spatial;
using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Shared.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// Advanced spatial search operations showcasing O(log N) + O(K) performance.
/// Leverages SQL Server R-Tree spatial indexes for logarithmic candidate retrieval,
/// then applies exact vector distance calculations on candidates only.
/// All intelligence lives in database layer with CLR functions.
/// </summary>
[ApiController]
[Route("api/v1/spatial")]
public class SpatialSearchController : ApiControllerBase
{
    private readonly string _connectionString;
    private readonly ILogger<SpatialSearchController> _logger;

    public SpatialSearchController(
        IConfiguration configuration,
        ILogger<SpatialSearchController> logger)
    {
        _connectionString = configuration.GetConnectionString("HartonomousDb")
            ?? throw new InvalidOperationException("Connection string 'HartonomousDb' not configured");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Hybrid search combining spatial R-Tree filtering with exact vector reranking.
    /// Stage 1: O(log N) spatial index retrieves candidates
    /// Stage 2: O(K) exact vector distance on candidates
    /// </summary>
    [HttpPost("hybrid")]
    [ProducesResponseType(typeof(ApiResponse<HybridSearchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HybridSearchResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<HybridSearchResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HybridSearchAsync(
        [FromBody] HybridSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.QueryVector == null || request.QueryVector.Length == 0)
        {
            return BadRequest(Failure<HybridSearchResponse>(new[] 
            { 
                ErrorDetailFactory.InvalidFieldValue("QueryVector", "Query vector cannot be empty") 
            }));
        }

        try
        {
            var results = new List<SpatialSearchResult>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("dbo.sp_HybridSearch", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

            // Build VECTOR type parameter (SQL Server 2022+)
            var vectorParam = command.Parameters.Add("@query_vector", SqlDbType.VarBinary);
            vectorParam.Value = SerializeVector(request.QueryVector);
            
            command.Parameters.AddWithValue("@query_dimension", request.QueryVector.Length);
            command.Parameters.AddWithValue("@query_spatial_x", request.SpatialQuery.X);
            command.Parameters.AddWithValue("@query_spatial_y", request.SpatialQuery.Y);
            command.Parameters.AddWithValue("@query_spatial_z", request.SpatialQuery.Z ?? 0.0);
            command.Parameters.AddWithValue("@spatial_candidates", request.SpatialCandidates ?? 100);
            command.Parameters.AddWithValue("@final_top_k", request.TopK ?? 10);
            command.Parameters.AddWithValue("@distance_metric", request.DistanceMetric ?? "cosine");
            command.Parameters.AddWithValue("@TenantId", request.TenantId ?? 1);
            
            if (request.EmbeddingType != null)
                command.Parameters.AddWithValue("@embedding_type", request.EmbeddingType);
            if (request.ModelId != null)
                command.Parameters.AddWithValue("@ModelId", request.ModelId.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new SpatialSearchResult
                {
                    AtomEmbeddingId = reader.GetInt64(reader.GetOrdinal("AtomEmbeddingId")),
                    AtomId = reader.GetInt64(reader.GetOrdinal("AtomId")),
                    Modality = reader.GetString(reader.GetOrdinal("Modality")),
                    Subtype = reader.IsDBNull(reader.GetOrdinal("Subtype")) ? null : reader.GetString(reader.GetOrdinal("Subtype")),
                    EmbeddingType = reader.IsDBNull(reader.GetOrdinal("EmbeddingType")) ? null : reader.GetString(reader.GetOrdinal("EmbeddingType")),
                    ModelId = reader.IsDBNull(reader.GetOrdinal("ModelId")) ? null : reader.GetInt32(reader.GetOrdinal("ModelId")),
                    ExactDistance = reader.GetDouble(reader.GetOrdinal("exact_distance")),
                    SpatialDistance = reader.GetDouble(reader.GetOrdinal("spatial_distance"))
                });
            }

            stopwatch.Stop();

            var response = new HybridSearchResponse
            {
                Results = results,
                TotalResults = results.Count,
                QueryTimeMs = stopwatch.ElapsedMilliseconds,
                PerformanceProfile = "O(log N) spatial filter + O(K) vector rerank"
            };

            _logger.LogInformation(
                "Hybrid search: {Results} results in {Ms}ms using {Candidates} spatial candidates",
                results.Count,
                stopwatch.ElapsedMilliseconds,
                request.SpatialCandidates ?? 100);

            return Ok(Success(response));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error during hybrid search");
            var error = ErrorDetailFactory.InternalServerError("SPATIAL_SEARCH_DB_ERROR", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                Failure<HybridSearchResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during hybrid search");
            var error = ErrorDetailFactory.InternalServerError("SPATIAL_SEARCH_FAILED", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                Failure<HybridSearchResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Cross-modal query: search across different modalities (text → image, image → audio, etc).
    /// Leverages shared geometric embedding space for true multi-modal reasoning.
    /// </summary>
    [HttpPost("cross-modal")]
    [ProducesResponseType(typeof(ApiResponse<CrossModalResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CrossModalResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CrossModalResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CrossModalSearchAsync(
        [FromBody] CrossModalRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var results = new List<CrossModalResult>();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("dbo.sp_CrossModalQuery", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

            if (!string.IsNullOrWhiteSpace(request.TextQuery))
                command.Parameters.AddWithValue("@text_query", request.TextQuery);
            
            if (request.SpatialQuery != null)
            {
                command.Parameters.AddWithValue("@spatial_query_x", request.SpatialQuery.X);
                command.Parameters.AddWithValue("@spatial_query_y", request.SpatialQuery.Y);
                if (request.SpatialQuery.Z.HasValue)
                    command.Parameters.AddWithValue("@spatial_query_z", request.SpatialQuery.Z.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.ModalityFilter))
                command.Parameters.AddWithValue("@modality_filter", request.ModalityFilter);

            command.Parameters.AddWithValue("@top_k", request.TopK ?? 10);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new CrossModalResult
                {
                    AtomEmbeddingId = reader.GetInt64(reader.GetOrdinal("AtomEmbeddingId")),
                    AtomId = reader.GetInt64(reader.GetOrdinal("AtomId")),
                    Modality = reader.GetString(reader.GetOrdinal("Modality")),
                    Subtype = reader.IsDBNull(reader.GetOrdinal("Subtype")) ? null : reader.GetString(reader.GetOrdinal("Subtype")),
                    CanonicalText = reader.IsDBNull(reader.GetOrdinal("CanonicalText")) ? null : reader.GetString(reader.GetOrdinal("CanonicalText")),
                    SpatialDistance = reader.IsDBNull(reader.GetOrdinal("SpatialDistance")) ? null : reader.GetDouble(reader.GetOrdinal("SpatialDistance"))
                });
            }

            var response = new CrossModalResponse
            {
                Results = results,
                TotalResults = results.Count,
                SourceModality = DetermineSourceModality(request),
                TargetModality = request.ModalityFilter
            };

            _logger.LogInformation(
                "Cross-modal search: {Results} results, {Source} → {Target}",
                results.Count,
                response.SourceModality,
                response.TargetModality ?? "all");

            return Ok(Success(response));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error during cross-modal search");
            var error = ErrorDetailFactory.InternalServerError("CROSS_MODAL_DB_ERROR", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                Failure<CrossModalResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during cross-modal search");
            var error = ErrorDetailFactory.InternalServerError("CROSS_MODAL_FAILED", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                Failure<CrossModalResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Fusion search combining vector, keyword, and spatial signals with configurable weights.
    /// Demonstrates true multi-signal fusion in database layer.
    /// </summary>
    [HttpPost("fusion")]
    [ProducesResponseType(typeof(ApiResponse<FusionSearchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FusionSearchResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<FusionSearchResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> FusionSearchAsync(
        [FromBody] FusionSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.QueryVector == null || request.QueryVector.Length == 0)
        {
            return BadRequest(Failure<FusionSearchResponse>(new[] 
            { 
                ErrorDetailFactory.InvalidFieldValue("QueryVector", "Query vector cannot be empty") 
            }));
        }

        // Validate weights sum to 1.0
        var weightSum = (request.VectorWeight ?? 0.5) + (request.KeywordWeight ?? 0.3) + (request.SpatialWeight ?? 0.2);
        if (Math.Abs(weightSum - 1.0) > 0.01)
        {
            return BadRequest(Failure<FusionSearchResponse>(new[] 
            { 
                ErrorDetailFactory.InvalidFieldValue("Weights", "Vector, Keyword, and Spatial weights must sum to 1.0") 
            }));
        }

        try
        {
            var results = new List<FusionSearchResult>();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("dbo.sp_FusionSearch", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

            var vectorParam = command.Parameters.Add("@QueryVector", SqlDbType.VarBinary);
            vectorParam.Value = SerializeVector(request.QueryVector);

            if (!string.IsNullOrWhiteSpace(request.Keywords))
                command.Parameters.AddWithValue("@Keywords", request.Keywords);

            if (request.SpatialRegion != null)
            {
                // Convert to WKT for GEOMETRY parameter
                var wkt = $"POLYGON(({string.Join(", ", request.SpatialRegion.Select(p => $"{p.X} {p.Y}"))}, {request.SpatialRegion[0].X} {request.SpatialRegion[0].Y}))";
                var geometryParam = command.Parameters.Add("@SpatialRegion", SqlDbType.Variant);
                geometryParam.UdtTypeName = "geometry";
                geometryParam.Value = Microsoft.SqlServer.Types.SqlGeometry.STGeomFromText(new System.Data.SqlTypes.SqlChars(wkt), 0);
            }

            command.Parameters.AddWithValue("@TopK", request.TopK ?? 10);
            command.Parameters.AddWithValue("@VectorWeight", request.VectorWeight ?? 0.5);
            command.Parameters.AddWithValue("@KeywordWeight", request.KeywordWeight ?? 0.3);
            command.Parameters.AddWithValue("@SpatialWeight", request.SpatialWeight ?? 0.2);

            if (request.TenantId.HasValue)
                command.Parameters.AddWithValue("@TenantId", request.TenantId.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new FusionSearchResult
                {
                    AtomId = reader.GetInt64(reader.GetOrdinal("AtomId")),
                    VectorScore = reader.GetDouble(reader.GetOrdinal("VectorScore")),
                    KeywordScore = reader.GetDouble(reader.GetOrdinal("KeywordScore")),
                    SpatialScore = reader.GetDouble(reader.GetOrdinal("SpatialScore")),
                    CombinedScore = reader.GetDouble(reader.GetOrdinal("CombinedScore")),
                    ContentHash = reader.GetString(reader.GetOrdinal("ContentHash")),
                    ContentType = reader.IsDBNull(reader.GetOrdinal("ContentType")) ? null : reader.GetString(reader.GetOrdinal("ContentType")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                });
            }

            var response = new FusionSearchResponse
            {
                Results = results,
                TotalResults = results.Count,
                Weights = new FusionWeights
                {
                    VectorWeight = request.VectorWeight ?? 0.5,
                    KeywordWeight = request.KeywordWeight ?? 0.3,
                    SpatialWeight = request.SpatialWeight ?? 0.2
                }
            };

            _logger.LogInformation(
                "Fusion search: {Results} results, weights: V={V:F2} K={K:F2} S={S:F2}",
                results.Count,
                response.Weights.VectorWeight,
                response.Weights.KeywordWeight,
                response.Weights.SpatialWeight);

            return Ok(Success(response));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error during fusion search");
            var error = ErrorDetailFactory.InternalServerError("FUSION_SEARCH_DB_ERROR", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                Failure<FusionSearchResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during fusion search");
            var error = ErrorDetailFactory.InternalServerError("FUSION_SEARCH_FAILED", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                Failure<FusionSearchResponse>(new[] { error }));
        }
    }

    private static byte[] SerializeVector(float[] vector)
    {
        // Serialize float array to binary for VECTOR type
        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static string DetermineSourceModality(CrossModalRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.TextQuery))
            return "text";
        if (request.SpatialQuery != null)
            return "spatial";
        return "unknown";
    }
}

using Hartonomous.Api.Common;
using Hartonomous.Api.DTOs.Analytics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Hartonomous.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly string _connectionString;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IConfiguration configuration,
        ILogger<AnalyticsController> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not configured");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("usage")]
    [ProducesResponseType(typeof(ApiResponse<UsageAnalyticsResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetUsageAnalytics(
        [FromBody] UsageAnalyticsRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Request body is required"));
        }

        if (request.EndDate < request.StartDate)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "EndDate must be after StartDate"));
        }

        if ((request.EndDate - request.StartDate).TotalDays > 365)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Date range cannot exceed 365 days"));
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var query = @"
                WITH TimeSeriesData AS (
                    SELECT 
                        CASE @GroupBy
                            WHEN 'day' THEN CAST(DATEADD(DAY, DATEDIFF(DAY, 0, ir.CreatedAt), 0) AS DATETIME)
                            WHEN 'week' THEN CAST(DATEADD(WEEK, DATEDIFF(WEEK, 0, ir.CreatedAt), 0) AS DATETIME)
                            WHEN 'month' THEN CAST(DATEADD(MONTH, DATEDIFF(MONTH, 0, ir.CreatedAt), 0) AS DATETIME)
                        END AS TimeBucket,
                        COUNT(*) AS RequestCount,
                        COUNT(DISTINCT ir.InferenceId) AS UniqueInferences,
                        AVG(ISNULL(ir.TotalDurationMs, 0)) AS AvgDuration
                    FROM dbo.InferenceRequests ir
                    WHERE ir.CreatedAt BETWEEN @StartDate AND @EndDate
                        AND (@Modality IS NULL OR ir.TaskType = @Modality)
                    GROUP BY 
                        CASE @GroupBy
                            WHEN 'day' THEN CAST(DATEADD(DAY, DATEDIFF(DAY, 0, ir.CreatedAt), 0) AS DATETIME)
                            WHEN 'week' THEN CAST(DATEADD(WEEK, DATEDIFF(WEEK, 0, ir.CreatedAt), 0) AS DATETIME)
                            WHEN 'month' THEN CAST(DATEADD(MONTH, DATEDIFF(MONTH, 0, ir.CreatedAt), 0) AS DATETIME)
                        END
                )
                SELECT 
                    TimeBucket,
                    RequestCount,
                    UniqueInferences,
                    0 AS DeduplicatedCount,
                    0.0 AS DeduplicationRate,
                    0 AS TotalBytes,
                    AvgDuration
                FROM TimeSeriesData
                ORDER BY TimeBucket;

                -- Summary stats
                SELECT 
                    COUNT(*) AS TotalRequests,
                    COUNT(DISTINCT InferenceId) AS TotalInferences,
                    0 AS TotalDeduped,
                    0.0 AS DeduplicationRate,
                    0 AS TotalBytes,
                    AVG(ISNULL(TotalDurationMs, 0)) AS AvgDuration
                FROM dbo.InferenceRequests
                WHERE CreatedAt BETWEEN @StartDate AND @EndDate
                    AND (@Modality IS NULL OR TaskType = @Modality);";

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StartDate", request.StartDate);
            command.Parameters.AddWithValue("@EndDate", request.EndDate);
            command.Parameters.AddWithValue("@Modality", request.Modality ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@GroupBy", request.GroupBy ?? "day");

            var dataPoints = new List<UsageDataPoint>();
            var summary = new UsageSummary();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            // Read time series data
            while (await reader.ReadAsync(cancellationToken))
            {
                dataPoints.Add(new UsageDataPoint
                {
                    Timestamp = reader.GetDateTime(0),
                    TotalRequests = reader.GetInt32(1),
                    UniqueAtoms = reader.GetInt32(2),
                    DeduplicatedCount = reader.GetInt32(3),
                    DeduplicationRate = reader.GetDouble(4),
                    TotalBytesProcessed = reader.GetInt64(5),
                    AvgResponseTimeMs = reader.GetDouble(6)
                });
            }

            // Read summary
            if (await reader.NextResultAsync(cancellationToken) && await reader.ReadAsync(cancellationToken))
            {
                summary = new UsageSummary
                {
                    TotalRequests = reader.GetInt32(0),
                    TotalAtoms = reader.GetInt32(1),
                    TotalDeduped = reader.GetInt32(2),
                    OverallDeduplicationRate = reader.GetDouble(3),
                    TotalBytesProcessed = reader.GetInt64(4),
                    AvgResponseTimeMs = reader.GetDouble(5)
                };
            }

            _logger.LogInformation("Usage analytics retrieved: {DataPoints} data points, {TotalRequests} total requests",
                dataPoints.Count, summary.TotalRequests);

            return Ok(ApiResponse<UsageAnalyticsResponse>.Ok(new UsageAnalyticsResponse
            {
                DataPoints = dataPoints,
                Summary = summary
            }, new ApiMetadata
            {
                TotalCount = dataPoints.Count,
                Extra = new Dictionary<string, object>
                {
                    ["dateRange"] = $"{request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}",
                    ["groupBy"] = request.GroupBy ?? "day"
                }
            }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error retrieving usage analytics");
            return StatusCode(500, ApiResponse<object>.Fail("DATABASE_ERROR", "Failed to retrieve analytics", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve usage analytics");
            return StatusCode(500, ApiResponse<object>.Fail("ANALYTICS_FAILED", ex.Message));
        }
    }

    [HttpPost("models/performance")]
    [ProducesResponseType(typeof(ApiResponse<ModelPerformanceResponse>), 200)]
    public async Task<IActionResult> GetModelPerformance(
        [FromBody] ModelPerformanceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var query = @"
                SELECT 
                    m.ModelId,
                    m.ModelName,
                    ISNULL(m.UsageCount, 0) AS TotalInferences,
                    AVG(ml.AvgComputeTimeMs) AS AvgInferenceTimeMs,
                    0.0 AS AvgConfidenceScore,
                    AVG(ISNULL(ml.CacheHitRate, 0)) AS CacheHitRate,
                    0 AS TotalTokensGenerated,
                    m.LastUsed,
                    m.UsageCount
                FROM dbo.Models m
                LEFT JOIN dbo.ModelLayers ml ON ml.ModelId = m.ModelId
                WHERE (@ModelId IS NULL OR m.ModelId = @ModelId)
                    AND (@StartDate IS NULL OR m.LastUsed >= @StartDate)
                    AND (@EndDate IS NULL OR m.LastUsed <= @EndDate)
                GROUP BY m.ModelId, m.ModelName, m.UsageCount, m.LastUsed
                ORDER BY m.UsageCount DESC;";

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ModelId", request?.ModelId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@StartDate", request?.StartDate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@EndDate", request?.EndDate ?? (object)DBNull.Value);

            var metrics = new List<ModelPerformanceMetric>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                metrics.Add(new ModelPerformanceMetric
                {
                    ModelId = reader.GetInt32(0),
                    ModelName = reader.GetString(1),
                    TotalInferences = reader.GetInt64(2),
                    AvgInferenceTimeMs = reader.IsDBNull(3) ? 0 : (double)reader.GetFloat(3),
                    AvgConfidenceScore = reader.GetDouble(4),
                    CacheHitRate = reader.IsDBNull(5) ? 0 : (double)reader.GetFloat(5),
                    TotalTokensGenerated = reader.GetInt64(6),
                    LastUsed = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                    UsageCount = reader.IsDBNull(8) ? null : reader.GetInt32(8)
                });
            }

            _logger.LogInformation("Model performance metrics retrieved for {Count} models", metrics.Count);

            return Ok(ApiResponse<ModelPerformanceResponse>.Ok(new ModelPerformanceResponse
            {
                Metrics = metrics
            }, new ApiMetadata
            {
                TotalCount = metrics.Count
            }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error retrieving model performance");
            return StatusCode(500, ApiResponse<object>.Fail("DATABASE_ERROR", "Failed to retrieve model performance", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve model performance");
            return StatusCode(500, ApiResponse<object>.Fail("ANALYTICS_FAILED", ex.Message));
        }
    }

    [HttpPost("embeddings/stats")]
    [ProducesResponseType(typeof(ApiResponse<EmbeddingStatsResponse>), 200)]
    public async Task<IActionResult> GetEmbeddingStats(
        [FromBody] EmbeddingStatsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var query = @"
                SELECT 
                    ae.EmbeddingType,
                    ae.ModelId,
                    m.ModelName,
                    COUNT(*) AS TotalEmbeddings,
                    COUNT(DISTINCT ae.AtomId) AS UniqueAtoms,
                    AVG(ae.Dimension) AS AvgDimension,
                    SUM(CASE WHEN ae.UsesMaxDimensionPadding = 1 THEN 1 ELSE 0 END) AS UsePaddingCount,
                    COUNT(DISTINCT aec.AtomEmbeddingId) AS ComponentStorageCount,
                    0.0 AS AvgSpatialDistance
                FROM dbo.AtomEmbeddings ae
                LEFT JOIN dbo.Models m ON m.ModelId = ae.ModelId
                LEFT JOIN dbo.AtomEmbeddingComponents aec ON aec.AtomEmbeddingId = ae.AtomEmbeddingId
                WHERE (@EmbeddingType IS NULL OR ae.EmbeddingType = @EmbeddingType)
                    AND (@ModelId IS NULL OR ae.ModelId = @ModelId)
                GROUP BY ae.EmbeddingType, ae.ModelId, m.ModelName
                ORDER BY TotalEmbeddings DESC;

                -- Overall stats
                SELECT 
                    COUNT(*) AS TotalEmbeddings,
                    COUNT(DISTINCT AtomId) AS UniqueAtoms,
                    COUNT(DISTINCT EmbeddingType) AS DistinctTypes,
                    COUNT(DISTINCT ModelId) AS DistinctModels
                FROM dbo.AtomEmbeddings
                WHERE (@EmbeddingType IS NULL OR EmbeddingType = @EmbeddingType)
                    AND (@ModelId IS NULL OR ModelId = @ModelId);";

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@EmbeddingType", request?.EmbeddingType ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ModelId", request?.ModelId ?? (object)DBNull.Value);

            var stats = new List<EmbeddingTypeStat>();
            var overall = new EmbeddingOverallStats();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            // Read per-type stats
            while (await reader.ReadAsync(cancellationToken))
            {
                stats.Add(new EmbeddingTypeStat
                {
                    EmbeddingType = reader.GetString(0),
                    ModelId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    ModelName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    TotalEmbeddings = reader.GetInt64(3),
                    UniqueAtoms = reader.GetInt64(4),
                    AvgDimension = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    UsePaddingCount = reader.GetInt64(6),
                    ComponentStorageCount = reader.GetInt64(7),
                    AvgSpatialDistance = reader.GetDouble(8)
                });
            }

            // Read overall stats
            if (await reader.NextResultAsync(cancellationToken) && await reader.ReadAsync(cancellationToken))
            {
                overall = new EmbeddingOverallStats
                {
                    TotalEmbeddings = reader.GetInt64(0),
                    UniqueAtoms = reader.GetInt64(1),
                    DistinctEmbeddingTypes = reader.GetInt32(2),
                    DistinctModels = reader.GetInt32(3)
                };
            }

            _logger.LogInformation("Embedding stats retrieved: {TypeCount} types, {TotalEmbeddings} total embeddings",
                stats.Count, overall.TotalEmbeddings);

            return Ok(ApiResponse<EmbeddingStatsResponse>.Ok(new EmbeddingStatsResponse
            {
                Stats = stats,
                Overall = overall
            }, new ApiMetadata
            {
                TotalCount = stats.Count
            }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error retrieving embedding stats");
            return StatusCode(500, ApiResponse<object>.Fail("DATABASE_ERROR", "Failed to retrieve embedding stats", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve embedding stats");
            return StatusCode(500, ApiResponse<object>.Fail("ANALYTICS_FAILED", ex.Message));
        }
    }

    [HttpGet("storage")]
    [ProducesResponseType(typeof(ApiResponse<StorageMetricsResponse>), 200)]
    public async Task<IActionResult> GetStorageMetrics(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var query = @"
                -- Count metrics
                SELECT 
                    (SELECT COUNT(*) FROM dbo.Atoms) AS TotalAtoms,
                    (SELECT COUNT(*) FROM dbo.AtomEmbeddings) AS TotalEmbeddings,
                    (SELECT COUNT(*) FROM dbo.TensorAtoms) AS TotalTensorAtoms,
                    (SELECT COUNT(*) FROM dbo.Models) AS TotalModels,
                    (SELECT COUNT(*) FROM dbo.ModelLayers) AS TotalLayers,
                    (SELECT COUNT(*) FROM dbo.InferenceRequests) AS TotalInferences;

                -- Storage size breakdown (simplified - would need actual table size queries)
                SELECT 
                    100 AS AtomTableSizeMB,
                    50 AS EmbeddingTableSizeMB,
                    200 AS TensorAtomTableSizeMB,
                    500 AS FilestreamSizeMB,
                    850 AS TotalDatabaseSizeMB;

                -- Deduplication metrics
                SELECT 
                    ISNULL(SUM(ReferenceCount), 0) AS TotalReferences,
                    COUNT(*) AS UniqueAtoms,
                    CASE WHEN COUNT(*) > 0 
                        THEN ((ISNULL(SUM(ReferenceCount), 0) - COUNT(*)) * 100.0 / ISNULL(SUM(ReferenceCount), 1))
                        ELSE 0 
                    END AS SpaceSavingsPercent,
                    0 AS EstimatedBytesSaved
                FROM dbo.Atoms;";

            await using var command = new SqlCommand(query, connection);

            var response = new StorageMetricsResponse();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            // Read counts
            if (await reader.ReadAsync(cancellationToken))
            {
                response.TotalAtoms = reader.GetInt32(0);
                response.TotalEmbeddings = reader.GetInt32(1);
                response.TotalTensorAtoms = reader.GetInt32(2);
                response.TotalModels = reader.GetInt32(3);
                response.TotalLayers = reader.GetInt32(4);
                response.TotalInferenceRequests = reader.GetInt32(5);
            }

            // Read size breakdown
            if (await reader.NextResultAsync(cancellationToken) && await reader.ReadAsync(cancellationToken))
            {
                response.SizeBreakdown = new StorageSizeBreakdown
                {
                    AtomTableSizeMB = reader.GetInt64(0),
                    EmbeddingTableSizeMB = reader.GetInt64(1),
                    TensorAtomTableSizeMB = reader.GetInt64(2),
                    FilestreamSizeMB = reader.GetInt64(3),
                    TotalDatabaseSizeMB = reader.GetInt64(4)
                };
            }

            // Read deduplication metrics
            if (await reader.NextResultAsync(cancellationToken) && await reader.ReadAsync(cancellationToken))
            {
                response.Deduplication = new DeduplicationMetrics
                {
                    TotalAtomReferences = reader.GetInt64(0),
                    UniqueAtoms = reader.GetInt64(1),
                    SpaceSavingsPercent = reader.GetDouble(2),
                    EstimatedBytesSaved = reader.GetInt64(3)
                };
            }

            _logger.LogInformation("Storage metrics retrieved: {TotalAtoms} atoms, {SpaceSavings:F1}% dedup savings",
                response.TotalAtoms, response.Deduplication.SpaceSavingsPercent);

            return Ok(ApiResponse<StorageMetricsResponse>.Ok(response, new ApiMetadata
            {
                Extra = new Dictionary<string, object>
                {
                    ["deduplicationEnabled"] = true,
                    ["totalSizeMB"] = response.SizeBreakdown.TotalDatabaseSizeMB
                }
            }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error retrieving storage metrics");
            return StatusCode(500, ApiResponse<object>.Fail("DATABASE_ERROR", "Failed to retrieve storage metrics", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve storage metrics");
            return StatusCode(500, ApiResponse<object>.Fail("ANALYTICS_FAILED", ex.Message));
        }
    }

    [HttpPost("top-atoms")]
    [ProducesResponseType(typeof(ApiResponse<TopAtomsResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetTopAtoms(
        [FromBody] TopAtomsRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Request body is required"));
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var orderByClause = request.OrderBy?.ToLower() switch
            {
                "embedding_count" => "EmbeddingCount DESC",
                "last_accessed" => "LastAccessed DESC",
                _ => "ReferenceCount DESC"
            };

            var query = $@"
                SELECT TOP (@TopK)
                    a.AtomId,
                    a.Modality,
                    a.CanonicalText,
                    a.ReferenceCount,
                    COUNT(DISTINCT ae.AtomEmbeddingId) AS EmbeddingCount,
                    MAX(ae.CreatedAt) AS LastAccessed,
                    AVG(ta.ImportanceScore) AS AvgImportance
                FROM dbo.Atoms a
                LEFT JOIN dbo.AtomEmbeddings ae ON ae.AtomId = a.AtomId
                LEFT JOIN dbo.TensorAtoms ta ON ta.AtomId = a.AtomId
                WHERE (@Modality IS NULL OR a.Modality = @Modality)
                GROUP BY a.AtomId, a.Modality, a.CanonicalText, a.ReferenceCount
                ORDER BY {orderByClause};";

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@TopK", request.TopK);
            command.Parameters.AddWithValue("@Modality", request.Modality ?? (object)DBNull.Value);

            var rankings = new List<AtomRankingEntry>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                rankings.Add(new AtomRankingEntry
                {
                    AtomId = reader.GetInt64(0),
                    Modality = reader.GetString(1),
                    CanonicalText = reader.IsDBNull(2) ? null : reader.GetString(2),
                    ReferenceCount = reader.GetInt64(3),
                    EmbeddingCount = reader.GetInt32(4),
                    LastAccessed = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    AvgImportanceScore = reader.IsDBNull(6) ? null : (double)reader.GetFloat(6)
                });
            }

            _logger.LogInformation("Top atoms retrieved: {Count} atoms ordered by {OrderBy}",
                rankings.Count, request.OrderBy ?? "reference_count");

            return Ok(ApiResponse<TopAtomsResponse>.Ok(new TopAtomsResponse
            {
                Rankings = rankings
            }, new ApiMetadata
            {
                TotalCount = rankings.Count,
                Extra = new Dictionary<string, object>
                {
                    ["orderBy"] = request.OrderBy ?? "reference_count",
                    ["modality"] = request.Modality ?? "all"
                }
            }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error retrieving top atoms");
            return StatusCode(500, ApiResponse<object>.Fail("DATABASE_ERROR", "Failed to retrieve top atoms", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve top atoms");
            return StatusCode(500, ApiResponse<object>.Fail("ANALYTICS_FAILED", ex.Message));
        }
    }
}

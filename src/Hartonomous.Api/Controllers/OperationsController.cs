using Hartonomous.Api.Common;
using Hartonomous.Api.DTOs.Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace Hartonomous.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OperationsController : ControllerBase
{
    private readonly string _connectionString;
    private readonly ILogger<OperationsController> _logger;

    public OperationsController(
        IConfiguration configuration,
        ILogger<OperationsController> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not configured");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<HealthCheckResponse>), 200)]
    public async Task<IActionResult> HealthCheck(CancellationToken cancellationToken)
    {
        var overallStopwatch = Stopwatch.StartNew();
        var components = new Dictionary<string, ComponentHealth>();

        // Check SQL Server connectivity
        var sqlHealth = await CheckSqlServerHealth(cancellationToken);
        components["sqlserver"] = sqlHealth;

        // Check critical tables
        var tablesHealth = await CheckCriticalTables(cancellationToken);
        components["tables"] = tablesHealth;

        // Check FILESTREAM
        var filestreamHealth = await CheckFilestreamHealth(cancellationToken);
        components["filestream"] = filestreamHealth;

        overallStopwatch.Stop();

        var overallStatus = components.Values.All(c => c.Status == "healthy") ? "healthy"
            : components.Values.Any(c => c.Status == "unhealthy") ? "unhealthy"
            : "degraded";

        var response = new HealthCheckResponse
        {
            Status = overallStatus,
            Components = components,
            CheckedAt = DateTime.UtcNow,
            TotalCheckDuration = overallStopwatch.Elapsed
        };

        var statusCode = overallStatus == "healthy" ? 200 : overallStatus == "degraded" ? 200 : 503;

        _logger.LogInformation("Health check completed: {Status} in {Duration}ms",
            overallStatus, overallStopwatch.ElapsedMilliseconds);

        return StatusCode(statusCode, ApiResponse<HealthCheckResponse>.Ok(response));
    }

    [HttpPost("indexes/maintenance")]
    [ProducesResponseType(typeof(ApiResponse<IndexMaintenanceResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> PerformIndexMaintenance(
        [FromBody] IndexMaintenanceRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Request body is required"));
        }

        if (!new[] { "rebuild", "reorganize", "update_statistics" }.Contains(request.Operation.ToLower()))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Operation must be rebuild, reorganize, or update_statistics"));
        }

        try
        {
            var totalStopwatch = Stopwatch.StartNew();
            var results = new List<IndexOperationResult>();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Get fragmented indexes if no specific index specified
            var indexQuery = request.IndexName != null
                ? @"SELECT i.name AS IndexName, OBJECT_NAME(i.object_id) AS TableName, ps.avg_fragmentation_in_percent
                    FROM sys.indexes i
                    INNER JOIN sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID(@TableName), NULL, NULL, 'LIMITED') ps 
                        ON i.object_id = ps.object_id AND i.index_id = ps.index_id
                    WHERE i.name = @IndexName AND i.type > 0"
                : @"SELECT i.name AS IndexName, OBJECT_NAME(i.object_id) AS TableName, ps.avg_fragmentation_in_percent
                    FROM sys.indexes i
                    INNER JOIN sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ps 
                        ON i.object_id = ps.object_id AND i.index_id = ps.index_id
                    WHERE ps.avg_fragmentation_in_percent > 10 
                        AND i.type > 0 
                        AND ps.page_count > 100
                        AND (@TableName IS NULL OR OBJECT_NAME(i.object_id) = @TableName)
                    ORDER BY ps.avg_fragmentation_in_percent DESC";

            await using var indexCmd = new SqlCommand(indexQuery, connection);
            indexCmd.Parameters.AddWithValue("@IndexName", request.IndexName ?? (object)DBNull.Value);
            indexCmd.Parameters.AddWithValue("@TableName", request.TableName ?? (object)DBNull.Value);

            var indexesToProcess = new List<(string IndexName, string TableName, double Fragmentation)>();
            await using var reader = await indexCmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                indexesToProcess.Add((
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetDouble(2)
                ));
            }
            reader.Close();

            // Process each index
            foreach (var (indexName, tableName, fragBefore) in indexesToProcess)
            {
                var opStopwatch = Stopwatch.StartNew();
                var result = new IndexOperationResult
                {
                    IndexName = indexName,
                    TableName = tableName,
                    Operation = request.Operation,
                    FragmentationBefore = fragBefore
                };

                try
                {
                    string maintenanceSql = request.Operation.ToLower() switch
                    {
                        "rebuild" => $"ALTER INDEX [{indexName}] ON [{tableName}] REBUILD {(request.Online ? "WITH (ONLINE = ON)" : "")}",
                        "reorganize" => $"ALTER INDEX [{indexName}] ON [{tableName}] REORGANIZE",
                        "update_statistics" => $"UPDATE STATISTICS [{tableName}] [{indexName}]",
                        _ => throw new InvalidOperationException("Invalid operation")
                    };

                    await using var maintenanceCmd = new SqlCommand(maintenanceSql, connection)
                    {
                        CommandTimeout = 600 // 10 minutes for large indexes
                    };

                    await maintenanceCmd.ExecuteNonQueryAsync(cancellationToken);

                    // Check fragmentation after
                    var fragCheckSql = @"SELECT avg_fragmentation_in_percent 
                        FROM sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID(@TableName), NULL, NULL, 'LIMITED')
                        WHERE index_id = (SELECT index_id FROM sys.indexes WHERE name = @IndexName AND object_id = OBJECT_ID(@TableName))";

                    await using var fragCmd = new SqlCommand(fragCheckSql, connection);
                    fragCmd.Parameters.AddWithValue("@TableName", tableName);
                    fragCmd.Parameters.AddWithValue("@IndexName", indexName);

                    var fragAfterObj = await fragCmd.ExecuteScalarAsync(cancellationToken);
                    result.FragmentationAfter = fragAfterObj != null && fragAfterObj != DBNull.Value 
                        ? Convert.ToDouble(fragAfterObj) 
                        : null;

                    opStopwatch.Stop();
                    result.Duration = opStopwatch.Elapsed;
                    result.Success = true;
                    result.Message = $"Completed successfully. Fragmentation: {fragBefore:F1}% → {result.FragmentationAfter:F1}%";

                    _logger.LogInformation("Index maintenance: {Operation} on {Table}.{Index} completed in {Duration}ms",
                        request.Operation, tableName, indexName, opStopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    opStopwatch.Stop();
                    result.Duration = opStopwatch.Elapsed;
                    result.Success = false;
                    result.Message = ex.Message;

                    _logger.LogError(ex, "Index maintenance failed for {Table}.{Index}", tableName, indexName);
                }

                results.Add(result);
            }

            totalStopwatch.Stop();

            return Ok(ApiResponse<IndexMaintenanceResponse>.Ok(new IndexMaintenanceResponse
            {
                Results = results,
                TotalDuration = totalStopwatch.Elapsed
            }, new ApiMetadata
            {
                TotalCount = results.Count,
                Extra = new Dictionary<string, object>
                {
                    ["successCount"] = results.Count(r => r.Success),
                    ["failureCount"] = results.Count(r => !r.Success)
                }
            }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error during index maintenance");
            return StatusCode(500, ApiResponse<object>.Fail("DATABASE_ERROR", "Index maintenance failed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Index maintenance failed");
            return StatusCode(500, ApiResponse<object>.Fail("MAINTENANCE_FAILED", ex.Message));
        }
    }

    [HttpPost("cache/manage")]
    [ProducesResponseType(typeof(ApiResponse<CacheManagementResponse>), 200)]
    public async Task<IActionResult> ManageCache(
        [FromBody] CacheManagementRequest request,
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

            var response = new CacheManagementResponse
            {
                Operation = request.Operation,
                Success = true
            };

            switch (request.Operation.ToLower())
            {
                case "clear":
                    await using (var cmd = new SqlCommand("DBCC DROPCLEANBUFFERS; DBCC FREEPROCCACHE;", connection))
                    {
                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                    }
                    response.Message = "Buffer pool and procedure cache cleared";
                    _logger.LogWarning("Cache cleared by admin request");
                    break;

                case "stats":
                    var stats = await GetCacheStats(connection, cancellationToken);
                    response.Stats = stats;
                    response.Message = "Cache statistics retrieved";
                    break;

                default:
                    response.Success = false;
                    response.Message = $"Unknown operation: {request.Operation}";
                    break;
            }

            return Ok(ApiResponse<CacheManagementResponse>.Ok(response));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error during cache management");
            return StatusCode(500, ApiResponse<object>.Fail("DATABASE_ERROR", "Cache management failed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache management failed");
            return StatusCode(500, ApiResponse<object>.Fail("CACHE_MANAGEMENT_FAILED", ex.Message));
        }
    }

    [HttpPost("diagnostics")]
    [ProducesResponseType(typeof(ApiResponse<DiagnosticResponse>), 200)]
    public async Task<IActionResult> RunDiagnostics(
        [FromBody] DiagnosticRequest request,
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

            var entries = new List<DiagnosticEntry>();

            switch (request.DiagnosticType.ToLower())
            {
                case "slow_queries":
                    entries = await GetSlowQueries(connection, request.TopK, cancellationToken);
                    break;

                case "blocking":
                    entries = await GetBlockingInfo(connection, cancellationToken);
                    break;

                case "resource_usage":
                    entries = await GetResourceUsage(connection, cancellationToken);
                    break;

                default:
                    return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", $"Unknown diagnostic type: {request.DiagnosticType}"));
            }

            _logger.LogInformation("Diagnostics completed: {Type} returned {Count} entries",
                request.DiagnosticType, entries.Count);

            return Ok(ApiResponse<DiagnosticResponse>.Ok(new DiagnosticResponse
            {
                DiagnosticType = request.DiagnosticType,
                Entries = entries
            }, new ApiMetadata
            {
                TotalCount = entries.Count
            }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error during diagnostics");
            return StatusCode(500, ApiResponse<object>.Fail("DATABASE_ERROR", "Diagnostics failed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostics failed");
            return StatusCode(500, ApiResponse<object>.Fail("DIAGNOSTICS_FAILED", ex.Message));
        }
    }

    [HttpGet("querystore/stats")]
    [ProducesResponseType(typeof(ApiResponse<QueryStoreStatsResponse>), 200)]
    public async Task<IActionResult> GetQueryStoreStats(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var query = @"
                SELECT 
                    desired_state_desc,
                    actual_state_desc,
                    current_storage_size_mb,
                    max_storage_size_mb,
                    (current_storage_size_mb * 100.0 / NULLIF(max_storage_size_mb, 0)) AS storage_percent
                FROM sys.database_query_store_options;

                SELECT 
                    COUNT(DISTINCT query_id) AS TotalQueries,
                    COUNT(DISTINCT plan_id) AS TotalPlans
                FROM sys.query_store_plan;

                SELECT TOP 10
                    q.query_id,
                    qt.query_sql_text,
                    rs.count_executions,
                    rs.avg_duration / 1000.0 AS avg_duration_ms,
                    rs.avg_cpu_time / 1000.0 AS avg_cpu_ms,
                    rs.avg_logical_io_reads,
                    rs.last_execution_time
                FROM sys.query_store_query q
                INNER JOIN sys.query_store_query_text qt ON qt.query_text_id = q.query_text_id
                INNER JOIN sys.query_store_plan p ON p.query_id = q.query_id
                INNER JOIN sys.query_store_runtime_stats rs ON rs.plan_id = p.plan_id
                ORDER BY rs.avg_duration DESC;";

            await using var command = new SqlCommand(query, connection);

            var response = new QueryStoreStatsResponse
            {
                OperationMode = "unknown",
                TopQueries = new List<TopQueryEntry>()
            };

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            // Read Query Store config
            if (await reader.ReadAsync(cancellationToken))
            {
                response.QueryStoreEnabled = reader.GetString(1) != "OFF";
                response.OperationMode = reader.GetString(1);
                response.CurrentStorageMB = reader.GetInt64(2);
                response.MaxStorageMB = reader.GetInt64(3);
                response.StorageUsedPercent = reader.GetDouble(4);
            }

            // Read counts
            if (await reader.NextResultAsync(cancellationToken) && await reader.ReadAsync(cancellationToken))
            {
                response.TotalQueries = reader.GetInt32(0);
                response.TotalPlans = reader.GetInt32(1);
            }

            // Read top queries
            if (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    response.TopQueries.Add(new TopQueryEntry
                    {
                        QueryId = reader.GetInt64(0),
                        QueryText = reader.GetString(1).Length > 500 ? reader.GetString(1).Substring(0, 500) + "..." : reader.GetString(1),
                        ExecutionCount = reader.GetInt64(2),
                        AvgDurationMs = reader.GetDouble(3),
                        AvgCpuTimeMs = reader.GetDouble(4),
                        AvgLogicalReads = reader.GetDouble(5),
                        LastExecutionTime = reader.GetDateTime(6)
                    });
                }
            }

            _logger.LogInformation("Query Store stats retrieved: {Queries} queries, {Plans} plans",
                response.TotalQueries, response.TotalPlans);

            return Ok(ApiResponse<QueryStoreStatsResponse>.Ok(response));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error retrieving Query Store stats");
            return StatusCode(500, ApiResponse<object>.Fail("DATABASE_ERROR", "Failed to retrieve Query Store stats", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Query Store stats");
            return StatusCode(500, ApiResponse<object>.Fail("QUERYSTORE_FAILED", ex.Message));
        }
    }

    // Helper methods
    private async Task<ComponentHealth> CheckSqlServerHealth(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand("SELECT @@VERSION", connection);
            var version = await command.ExecuteScalarAsync(cancellationToken);

            stopwatch.Stop();
            return new ComponentHealth
            {
                Status = "healthy",
                Message = "SQL Server is responsive",
                ResponseTime = stopwatch.Elapsed,
                Data = new Dictionary<string, object> { ["version"] = version?.ToString() ?? "unknown" }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ComponentHealth
            {
                Status = "unhealthy",
                Message = ex.Message,
                ResponseTime = stopwatch.Elapsed
            };
        }
    }

    private async Task<ComponentHealth> CheckCriticalTables(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            
            var query = @"SELECT 
                (SELECT COUNT(*) FROM dbo.Atoms) AS AtomCount,
                (SELECT COUNT(*) FROM dbo.AtomEmbeddings) AS EmbeddingCount,
                (SELECT COUNT(*) FROM dbo.Models) AS ModelCount";

            await using var command = new SqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                stopwatch.Stop();
                return new ComponentHealth
                {
                    Status = "healthy",
                    Message = "Critical tables accessible",
                    ResponseTime = stopwatch.Elapsed,
                    Data = new Dictionary<string, object>
                    {
                        ["atomCount"] = reader.GetInt32(0),
                        ["embeddingCount"] = reader.GetInt32(1),
                        ["modelCount"] = reader.GetInt32(2)
                    }
                };
            }

            stopwatch.Stop();
            return new ComponentHealth { Status = "degraded", Message = "No data returned", ResponseTime = stopwatch.Elapsed };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ComponentHealth { Status = "unhealthy", Message = ex.Message, ResponseTime = stopwatch.Elapsed };
        }
    }

    private async Task<ComponentHealth> CheckFilestreamHealth(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            
            var query = "SELECT SERVERPROPERTY('FilestreamEffectiveLevel') AS FilestreamLevel";
            await using var command = new SqlCommand(query, connection);
            var level = await command.ExecuteScalarAsync(cancellationToken);

            stopwatch.Stop();
            var filestreamLevel = Convert.ToInt32(level);
            
            return new ComponentHealth
            {
                Status = filestreamLevel > 0 ? "healthy" : "degraded",
                Message = filestreamLevel > 0 ? "FILESTREAM is enabled" : "FILESTREAM is disabled",
                ResponseTime = stopwatch.Elapsed,
                Data = new Dictionary<string, object> { ["level"] = filestreamLevel }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ComponentHealth { Status = "unhealthy", Message = ex.Message, ResponseTime = stopwatch.Elapsed };
        }
    }

    private async Task<CacheStats> GetCacheStats(SqlConnection connection, CancellationToken cancellationToken)
    {
        var query = @"
            SELECT 
                COUNT(*) AS PageCount,
                SUM(CAST(size_in_bytes AS BIGINT)) / 1024 / 1024 AS MemoryMB
            FROM sys.dm_os_buffer_descriptors
            WHERE database_id = DB_ID();";

        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return new CacheStats
            {
                TotalEntries = reader.GetInt64(0),
                HitCount = 0,
                MissCount = 0,
                HitRate = 0,
                MemoryUsedMB = reader.GetInt64(1)
            };
        }

        return new CacheStats();
    }

    private async Task<List<DiagnosticEntry>> GetSlowQueries(SqlConnection connection, int topK, CancellationToken cancellationToken)
    {
        var query = $@"
            SELECT TOP {topK}
                qt.query_sql_text,
                rs.count_executions,
                rs.avg_duration / 1000.0 AS avg_duration_ms,
                rs.last_execution_time
            FROM sys.query_store_query q
            INNER JOIN sys.query_store_query_text qt ON qt.query_text_id = q.query_text_id
            INNER JOIN sys.query_store_plan p ON p.query_id = q.query_id
            INNER JOIN sys.query_store_runtime_stats rs ON rs.plan_id = p.plan_id
            ORDER BY rs.avg_duration DESC;";

        await using var command = new SqlCommand(query, connection);
        var entries = new List<DiagnosticEntry>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new DiagnosticEntry
            {
                Category = "slow_query",
                Description = $"Query executed {reader.GetInt64(1)} times with avg duration {reader.GetDouble(2):F2}ms",
                Timestamp = reader.GetDateTime(3),
                Duration = TimeSpan.FromMilliseconds(reader.GetDouble(2)),
                Query = reader.GetString(0).Length > 1000 ? reader.GetString(0).Substring(0, 1000) + "..." : reader.GetString(0),
                Metrics = new Dictionary<string, object>
                {
                    ["executionCount"] = reader.GetInt64(1),
                    ["avgDurationMs"] = reader.GetDouble(2)
                }
            });
        }

        return entries;
    }

    private async Task<List<DiagnosticEntry>> GetBlockingInfo(SqlConnection connection, CancellationToken cancellationToken)
    {
        var query = @"
            SELECT 
                blocking_session_id,
                session_id,
                wait_type,
                wait_time,
                wait_resource
            FROM sys.dm_exec_requests
            WHERE blocking_session_id > 0;";

        await using var command = new SqlCommand(query, connection);
        var entries = new List<DiagnosticEntry>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new DiagnosticEntry
            {
                Category = "blocking",
                Description = $"Session {reader.GetInt32(1)} blocked by session {reader.GetInt32(0)}",
                Timestamp = DateTime.UtcNow,
                Duration = TimeSpan.FromMilliseconds(reader.GetInt32(3)),
                Metrics = new Dictionary<string, object>
                {
                    ["blockingSessionId"] = reader.GetInt32(0),
                    ["blockedSessionId"] = reader.GetInt32(1),
                    ["waitType"] = reader.GetString(2),
                    ["waitTimeMs"] = reader.GetInt32(3),
                    ["resource"] = reader.GetString(4)
                }
            });
        }

        return entries;
    }

    private async Task<List<DiagnosticEntry>> GetResourceUsage(SqlConnection connection, CancellationToken cancellationToken)
    {
        var query = @"
            SELECT 
                (SELECT cntr_value FROM sys.dm_os_performance_counters WHERE counter_name = 'Page life expectancy') AS PLE,
                (SELECT cntr_value FROM sys.dm_os_performance_counters WHERE counter_name = 'Buffer cache hit ratio' AND object_name LIKE '%Buffer Manager%') AS BufferCacheHitRatio,
                (SELECT SUM(size_in_bytes) / 1024 / 1024 FROM sys.dm_os_buffer_descriptors) AS BufferPoolMB;";

        await using var command = new SqlCommand(query, connection);
        var entries = new List<DiagnosticEntry>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new DiagnosticEntry
            {
                Category = "resource_usage",
                Description = "Current resource utilization metrics",
                Timestamp = DateTime.UtcNow,
                Metrics = new Dictionary<string, object>
                {
                    ["pageLifeExpectancy"] = reader.IsDBNull(0) ? 0 : reader.GetInt64(0),
                    ["bufferCacheHitRatio"] = reader.IsDBNull(1) ? 0 : reader.GetInt64(1),
                    ["bufferPoolMB"] = reader.IsDBNull(2) ? 0 : reader.GetInt64(2)
                }
            });
        }

        return entries;
    }
}

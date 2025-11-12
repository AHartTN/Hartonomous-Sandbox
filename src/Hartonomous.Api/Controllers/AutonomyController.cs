using Hartonomous.Api.DTOs.Autonomy;
using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Shared.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.Json;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// Autonomous OODA Loop (Observe-Orient-Decide-Act) Controller
/// Triggers and monitors the self-improving autonomous system
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Admin")] // Autonomous operations require admin privileges
public class AutonomyController : ApiControllerBase
{
    private readonly string _connectionString;
    private readonly ILogger<AutonomyController> _logger;

    public AutonomyController(IConfiguration configuration, ILogger<AutonomyController> logger)
    {
        _connectionString = configuration.GetConnectionString("HartonomousDb")
            ?? throw new InvalidOperationException("Connection string 'HartonomousDb' not found");
        _logger = logger;
    }

    /// <summary>
    /// Trigger the OODA loop Analyze phase
    /// Starts autonomous observation and pattern detection
    /// </summary>
    [HttpPost("ooda/analyze")]
    [ProducesResponseType(typeof(ApiResponse<AnalysisResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AnalysisResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TriggerAnalysisAsync([FromBody] TriggerAnalysisRequest request)
    {
        try
        {
            _logger.LogInformation("Triggering OODA Analyze phase for tenant {TenantId}, scope: {AnalysisScope}", 
                request.TenantId, request.AnalysisScope);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand("dbo.sp_Analyze", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure,
                CommandTimeout = 300 // 5 minutes for analysis
            };

            command.Parameters.AddWithValue("@TenantId", request.TenantId);
            command.Parameters.AddWithValue("@AnalysisScope", request.AnalysisScope);
            command.Parameters.AddWithValue("@LookbackHours", request.LookbackHours);

            var returnValue = command.Parameters.Add("@ReturnValue", System.Data.SqlDbType.Int);
            returnValue.Direction = System.Data.ParameterDirection.ReturnValue;

            // Execute and capture PRINT messages
            var messages = new StringBuilder();
            connection.InfoMessage += (sender, e) => messages.AppendLine(e.Message);

            await command.ExecuteNonQueryAsync();

            var result = (int)returnValue.Value;
            var outputMessages = messages.ToString();

            _logger.LogInformation("sp_Analyze completed with return code {ReturnCode}. Output: {Output}", 
                result, outputMessages);

            if (result != 0)
            {
                var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Analysis failed", $"sp_Analyze returned code {result}");
                return BadRequest(Failure<AnalysisResponse>(new[] { error }));
            }

            // Parse observations from PRINT output
            // Output format: "Observations: {json}"
            var observationsLine = outputMessages.Split('\n')
                .FirstOrDefault(line => line.StartsWith("Observations:"));

            if (observationsLine == null)
            {
                var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to parse analysis observations");
                return BadRequest(Failure<AnalysisResponse>(new[] { error }));
            }

            var observationsJson = observationsLine.Substring("Observations:".Length).Trim();
            var observations = JsonSerializer.Deserialize<JsonElement>(observationsJson);

            var response = new AnalysisResponse
            {
                AnalysisId = Guid.Parse(observations.GetProperty("analysisId").GetString()!),
                AnalysisScope = observations.GetProperty("scope").GetString()!,
                TotalInferences = observations.GetProperty("totalInferences").GetInt32(),
                AvgDurationMs = observations.GetProperty("avgDurationMs").GetDouble(),
                AnomalyCount = observations.GetProperty("anomalyCount").GetInt32(),
                PatternCount = observations.TryGetProperty("patterns", out var patterns) 
                    ? JsonSerializer.Deserialize<JsonElement[]>(patterns.GetRawText())?.Length ?? 0 
                    : 0,
                Observations = observationsJson,
                TimestampUtc = DateTime.Parse(observations.GetProperty("timestamp").GetString()!)
            };

            _logger.LogInformation("Analysis {AnalysisId} complete: {AnomalyCount} anomalies, {PatternCount} patterns detected",
                response.AnalysisId, response.AnomalyCount, response.PatternCount);

            return Ok(Success(response));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error during OODA Analyze phase");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to execute analysis", ex.Message);
            return BadRequest(Failure<AnalysisResponse>(new[] { error }));
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse analysis observations JSON");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to parse analysis observations");
            return BadRequest(Failure<AnalysisResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during OODA Analyze phase");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "An unexpected error occurred during analysis");
            return BadRequest(Failure<AnalysisResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Get Service Broker queue statuses
    /// Monitor OODA loop message flow
    /// </summary>
    [HttpGet("queues/status")]
    [ProducesResponseType(typeof(ApiResponse<List<QueueStatusResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQueueStatusAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving Service Broker queue statuses");

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT 
                    q.name AS QueueName,
                    ISNULL((SELECT COUNT(*) FROM sys.transmission_queue tq WHERE tq.service_name = s.name), 0) AS MessageCount,
                    (SELECT COUNT(DISTINCT conversation_handle) 
                     FROM sys.conversation_endpoints ce 
                     WHERE ce.service_id = s.service_id 
                       AND ce.state <> 'CD') AS ConversationCount,
                    (SELECT MAX(queuing_order) FROM sys.transmission_queue tq WHERE tq.service_name = s.name) AS LastMessageTime
                FROM sys.service_queues q
                INNER JOIN sys.services s ON q.object_id = s.service_queue_id
                WHERE q.name IN ('AnalyzeQueue', 'HypothesizeQueue', 'ActQueue', 'LearnQueue')
                ORDER BY q.name";

            await using var command = new SqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync();

            var queues = new List<QueueStatusResponse>();
            while (await reader.ReadAsync())
            {
                queues.Add(new QueueStatusResponse
                {
                    QueueName = reader.GetString(0),
                    MessageCount = reader.GetInt32(1),
                    ConversationCount = reader.GetInt32(2),
                    LastMessageUtc = reader.IsDBNull(3) ? null : reader.GetDateTime(3)
                });
            }

            _logger.LogInformation("Retrieved status for {QueueCount} Service Broker queues", queues.Count);

            return Ok(Success(queues));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error retrieving queue statuses");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to query queue statuses", ex.Message);
            return BadRequest(Failure<List<QueueStatusResponse>>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving queue statuses");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "An unexpected error occurred while retrieving queue statuses");
            return BadRequest(Failure<List<QueueStatusResponse>>(new[] { error }));
        }
    }

    /// <summary>
    /// Get OODA cycle history
    /// View past autonomous improvement cycles
    /// </summary>
    [HttpGet("cycles/history")]
    [ProducesResponseType(typeof(ApiResponse<OodaCycleHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCycleHistoryAsync(
        [FromQuery] int limit = 20,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            _logger.LogInformation("Retrieving OODA cycle history (limit: {Limit}, startDate: {StartDate}, endDate: {EndDate})", 
                limit, startDate, endDate);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT TOP (@Limit)
                    ImprovementId,
                    AnalysisResults,
                    ChangeType,
                    SuccessScore,
                    TestsPassed,
                    TestsFailed,
                    PerformanceDelta,
                    WasDeployed,
                    WasRolledBack,
                    StartedAt,
                    CompletedAt
                FROM dbo.AutonomousImprovementHistory
                WHERE (@StartDate IS NULL OR StartedAt >= @StartDate)
                    AND (@EndDate IS NULL OR StartedAt <= @EndDate)
                ORDER BY StartedAt DESC";

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Limit", limit);
            command.Parameters.AddWithValue("@StartDate", (object?)startDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@EndDate", (object?)endDate ?? DBNull.Value);

            var cycles = new List<OodaCycleRecord>();
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var cycle = new OodaCycleRecord
                {
                    AnalysisId = reader.GetGuid(0),
                    StartTimeUtc = reader.GetDateTime(9),
                    EndTimeUtc = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                    HypothesesGenerated = !reader.IsDBNull(1) ? 1 : 0, // Count from AnalysisResults JSON
                    ActionsExecuted = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                    LatencyImprovement = reader.IsDBNull(6) ? null : (double?)reader.GetDecimal(6),
                    Status = reader.GetBoolean(8) ? "RolledBack" : 
                             reader.GetBoolean(7) ? "Deployed" : 
                             !reader.IsDBNull(10) ? "Completed" : "InProgress"
                };

                cycles.Add(cycle);
            }

            var response = new OodaCycleHistoryResponse
            {
                Cycles = cycles,
                TotalCycles = cycles.Count,
                AvgLatencyImprovement = cycles.Count > 0 ? cycles.Average(c => c.LatencyImprovement ?? 0) : 0
            };

            _logger.LogInformation("Retrieved {CycleCount} OODA cycles from AutonomousImprovementHistory", response.TotalCycles);

            return Ok(Success(response));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error retrieving OODA cycle history");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to query cycle history", ex.Message);
            return BadRequest(Failure<OodaCycleHistoryResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving OODA cycle history");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "An unexpected error occurred while retrieving cycle history");
            return BadRequest(Failure<OodaCycleHistoryResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Pause the autonomous OODA loop
    /// Stop Service Broker queues to halt autonomous operations
    /// </summary>
    [HttpPost("control/pause")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PauseAutonomyAsync()
    {
        try
        {
            _logger.LogWarning("Pausing autonomous OODA loop");

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var commands = new[]
            {
                "ALTER QUEUE AnalyzeQueue WITH STATUS = OFF",
                "ALTER QUEUE HypothesizeQueue WITH STATUS = OFF",
                "ALTER QUEUE ActQueue WITH STATUS = OFF",
                "ALTER QUEUE LearnQueue WITH STATUS = OFF"
            };

            foreach (var commandText in commands)
            {
                await using var command = new SqlCommand(commandText, connection);
                await command.ExecuteNonQueryAsync();
            }

            _logger.LogWarning("All OODA loop queues paused successfully");

            return Ok(Success("Autonomous OODA loop paused. All queues stopped."));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Failed to pause OODA loop");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to pause autonomous operations", ex.Message);
            return BadRequest(Failure<string>(new[] { error }));
        }
    }

    /// <summary>
    /// Resume the autonomous OODA loop
    /// Restart Service Broker queues
    /// </summary>
    [HttpPost("control/resume")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResumeAutonomyAsync()
    {
        try
        {
            _logger.LogInformation("Resuming autonomous OODA loop");

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var commands = new[]
            {
                "ALTER QUEUE AnalyzeQueue WITH STATUS = ON",
                "ALTER QUEUE HypothesizeQueue WITH STATUS = ON",
                "ALTER QUEUE ActQueue WITH STATUS = ON",
                "ALTER QUEUE LearnQueue WITH STATUS = ON"
            };

            foreach (var commandText in commands)
            {
                await using var command = new SqlCommand(commandText, connection);
                await command.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("All OODA loop queues resumed successfully");

            return Ok(Success("Autonomous OODA loop resumed. All queues active."));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Failed to resume OODA loop");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to resume autonomous operations", ex.Message);
            return BadRequest(Failure<string>(new[] { error }));
        }
    }

    /// <summary>
    /// Clear all OODA loop conversations
    /// Emergency reset for stuck conversations
    /// </summary>
    [HttpPost("control/reset")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetConversationsAsync()
    {
        try
        {
            _logger.LogWarning("Resetting all OODA loop conversations");

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var resetScript = @"
                DECLARE @handle UNIQUEIDENTIFIER;
                DECLARE conv_cursor CURSOR FOR 
                    SELECT conversation_handle 
                    FROM sys.conversation_endpoints 
                    WHERE service_id IN (
                        SELECT service_id FROM sys.services 
                        WHERE name IN ('AnalyzeService', 'HypothesizeService', 'ActService', 'LearnService')
                    );
                
                OPEN conv_cursor;
                FETCH NEXT FROM conv_cursor INTO @handle;
                
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    END CONVERSATION @handle WITH CLEANUP;
                    FETCH NEXT FROM conv_cursor INTO @handle;
                END
                
                CLOSE conv_cursor;
                DEALLOCATE conv_cursor;
                
                SELECT @@ROWCOUNT AS ConversationsEnded;
            ";

            await using var command = new SqlCommand(resetScript, connection);
            var conversationsEnded = await command.ExecuteScalarAsync();

            _logger.LogWarning("Reset {ConversationCount} OODA loop conversations", conversationsEnded);

            return Ok(Success($"Reset complete. Ended {conversationsEnded} conversations."));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Failed to reset OODA loop conversations");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to reset conversations", ex.Message);
            return BadRequest(Failure<string>(new[] { error }));
        }
    }
}

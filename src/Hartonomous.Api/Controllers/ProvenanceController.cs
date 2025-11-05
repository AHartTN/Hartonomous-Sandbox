using Hartonomous.Api.Common;
using Hartonomous.Api.DTOs.Provenance;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Hartonomous.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProvenanceController : ControllerBase
{
    private readonly string _connectionString;
    private readonly ILogger<ProvenanceController> _logger;

    public ProvenanceController(
        IConfiguration configuration,
        ILogger<ProvenanceController> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not configured");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("streams/{streamId}")]
    [ProducesResponseType(typeof(ApiResponse<GenerationStreamDetail>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetGenerationStream(
        Guid streamId,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var query = @"
                SELECT 
                    StreamId,
                    Scope,
                    Model,
                    CreatedUtc,
                    Stream
                FROM provenance.GenerationStreams
                WHERE StreamId = @StreamId";

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StreamId", streamId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                var detail = new GenerationStreamDetail
                {
                    StreamId = reader.GetGuid(0),
                    Scope = reader.GetString(1),
                    Model = reader.IsDBNull(2) ? null : reader.GetString(2),
                    CreatedUtc = reader.GetDateTime(3),
                    StreamData = reader.IsDBNull(4) ? null : (byte[])reader.GetValue(4)
                };

                return Ok(ApiResponse<GenerationStreamDetail>.Ok(detail));
            }

            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", $"Generation stream {streamId} not found"));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error retrieving generation stream {StreamId}", streamId);
            return StatusCode(500, ApiResponse<object>.Fail("DATABASE_ERROR", "Failed to retrieve stream", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve generation stream {StreamId}", streamId);
            return StatusCode(500, ApiResponse<object>.Fail("QUERY_FAILED", ex.Message));
        }
    }

    [HttpGet("inference/{inferenceId}")]
    [ProducesResponseType(typeof(ApiResponse<InferenceDetail>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetInferenceDetail(
        long inferenceId,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var query = @"
                SELECT 
                    ir.InferenceId,
                    ir.TaskType,
                    ir.InputData,
                    ir.OutputData,
                    ir.ModelsUsed,
                    ir.EnsembleStrategy,
                    ir.TotalDurationMs,
                    ir.OutputMetadata,
                    ir.CreatedAt,
                    (SELECT COUNT(*) FROM dbo.InferenceSteps WHERE InferenceId = ir.InferenceId) AS StepCount
                FROM dbo.InferenceRequests ir
                WHERE ir.InferenceId = @InferenceId";

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@InferenceId", inferenceId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                var detail = new InferenceDetail
                {
                    InferenceId = reader.GetInt64(0),
                    TaskType = reader.GetString(1),
                    InputDataJson = reader.IsDBNull(2) ? null : reader.GetString(2),
                    OutputDataJson = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ModelsUsedJson = reader.IsDBNull(4) ? null : reader.GetString(4),
                    EnsembleStrategy = reader.IsDBNull(5) ? null : reader.GetString(5),
                    TotalDurationMs = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    OutputMetadataJson = reader.IsDBNull(7) ? null : reader.GetString(7),
                    CreatedAt = reader.GetDateTime(8),
                    StepCount = reader.GetInt32(9)
                };

                return Ok(ApiResponse<InferenceDetail>.Ok(detail));
            }

            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", $"Inference {inferenceId} not found"));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error retrieving inference {InferenceId}", inferenceId);
            return StatusCode(500, ApiResponse<object>.Fail("DATABASE_ERROR", "Failed to retrieve inference", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve inference {InferenceId}", inferenceId);
            return StatusCode(500, ApiResponse<object>.Fail("QUERY_FAILED", ex.Message));
        }
    }

    [HttpGet("inference/{inferenceId}/steps")]
    [ProducesResponseType(typeof(ApiResponse<List<InferenceStepDetail>>), 200)]
    public async Task<IActionResult> GetInferenceSteps(
        long inferenceId,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var query = @"
                SELECT 
                    InferenceStepId,
                    StepNumber,
                    ModelId,
                    OperationType,
                    DurationMs,
                    RowsReturned,
                    Metadata
                FROM dbo.InferenceSteps
                WHERE InferenceId = @InferenceId
                ORDER BY StepNumber";

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@InferenceId", inferenceId);

            var steps = new List<InferenceStepDetail>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                steps.Add(new InferenceStepDetail
                {
                    InferenceStepId = reader.GetInt64(0),
                    StepNumber = reader.GetInt32(1),
                    ModelId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    OperationType = reader.IsDBNull(3) ? null : reader.GetString(3),
                    DurationMs = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    RowsReturned = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    MetadataJson = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }

            return Ok(ApiResponse<List<InferenceStepDetail>>.Ok(steps, new ApiMetadata
            {
                TotalCount = steps.Count,
                Extra = new Dictionary<string, object> { ["inferenceId"] = inferenceId }
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve inference steps for {InferenceId}", inferenceId);
            return StatusCode(500, ApiResponse<object>.Fail("QUERY_FAILED", ex.Message));
        }
    }
}

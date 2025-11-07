using Hartonomous.Api.Common;
using Hartonomous.Api.DTOs.Feedback;
using Hartonomous.Infrastructure.Data.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace Hartonomous.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class FeedbackController : ControllerBase
{
    private readonly string _connectionString;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(
        IConfiguration configuration,
        ILogger<FeedbackController> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not configured");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("submit")]
    [ProducesResponseType(typeof(ApiResponse<SubmitFeedbackResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> SubmitFeedback(
        [FromBody] SubmitFeedbackRequest request,
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

            // Insert feedback record
            var insertQuery = @"
                INSERT INTO dbo.InferenceFeedback (InferenceId, Rating, FeedbackText, SubmittedAt, Metadata)
                OUTPUT INSERTED.FeedbackId
                VALUES (@InferenceId, @Rating, @FeedbackText, GETUTCDATE(), @Metadata);";

            long feedbackId;
            await using (var command = new SqlCommand(insertQuery, connection))
            {
                command.Parameters.Add("@InferenceId", SqlDbType.BigInt).Value = request.InferenceId;
                command.Parameters.Add("@Rating", SqlDbType.Int).Value = request.Rating;
                command.Parameters.Add("@FeedbackText", SqlDbType.NVarChar).Value = request.FeedbackText ?? (object)DBNull.Value;
                command.Parameters.Add("@Metadata", SqlDbType.NVarChar).Value = 
                    request.Metadata != null ? System.Text.Json.JsonSerializer.Serialize(request.Metadata) : (object)DBNull.Value;

                feedbackId = (long)await command.ExecuteScalarAsync(cancellationToken);
            }

            // Update importance scores for correct/incorrect atoms
            if (request.CorrectAtomIds?.Count > 0)
            {
                await UpdateAtomImportance(connection, request.CorrectAtomIds, 0.1, cancellationToken);
            }

            if (request.IncorrectAtomIds?.Count > 0)
            {
                await UpdateAtomImportance(connection, request.IncorrectAtomIds, -0.1, cancellationToken);
            }

            _logger.LogInformation("Feedback {FeedbackId} submitted for inference {InferenceId} with rating {Rating}",
                feedbackId, request.InferenceId, request.Rating);

            return Ok(ApiResponse<SubmitFeedbackResponse>.Ok(new SubmitFeedbackResponse
            {
                FeedbackId = feedbackId,
                Status = "recorded",
                Message = "Feedback recorded successfully. Importance scores updated."
            }, new ApiMetadata
            {
                Extra = new Dictionary<string, object>
                {
                    ["correctAtomsUpdated"] = request.CorrectAtomIds?.Count ?? 0,
                    ["incorrectAtomsUpdated"] = request.IncorrectAtomIds?.Count ?? 0
                }
            }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error submitting feedback");
            return StatusCode(500, ApiResponse<object>.Fail("DATABASE_ERROR", "Failed to submit feedback", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit feedback");
            return StatusCode(500, ApiResponse<object>.Fail("FEEDBACK_FAILED", ex.Message));
        }
    }

    [HttpPost("importance/update")]
    [ProducesResponseType(typeof(ApiResponse<UpdateImportanceResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> UpdateImportance(
        [FromBody] UpdateImportanceRequest request,
        CancellationToken cancellationToken)
    {
        if (request?.Updates == null || request.Updates.Count == 0)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "At least one importance update is required"));
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var results = new List<ImportanceUpdateResult>();

            foreach (var update in request.Updates)
            {
                var result = new ImportanceUpdateResult { AtomId = update.AtomId };

                try
                {
                    // Get current importance from TensorAtoms
                    var getQuery = "SELECT ImportanceScore FROM dbo.TensorAtoms WHERE AtomId = @AtomId";
                    await using var getCmd = new SqlCommand(getQuery, connection);
                    getCmd.Parameters.Add("@AtomId", SqlDbType.BigInt).Value = update.AtomId;

                    var currentImportanceObj = await getCmd.ExecuteScalarAsync(cancellationToken);
                    double currentImportance = currentImportanceObj != null && currentImportanceObj != DBNull.Value 
                        ? Convert.ToDouble(currentImportanceObj) 
                        : 0.5;

                    result.PreviousImportance = currentImportance;
                    var newImportance = Math.Clamp(currentImportance + update.ImportanceDelta, 0.0, 1.0);
                    result.NewImportance = newImportance;

                    // Update importance score
                    var updateQuery = @"
                        UPDATE dbo.TensorAtoms 
                        SET ImportanceScore = @NewImportance,
                            LastModified = GETUTCDATE()
                        WHERE AtomId = @AtomId";

                    await using var updateCmd = new SqlCommand(updateQuery, connection);
                    updateCmd.Parameters.Add("@AtomId", SqlDbType.BigInt).Value = update.AtomId;
                    updateCmd.Parameters.Add("@NewImportance", SqlDbType.Float).Value = newImportance;

                    var rowsAffected = await updateCmd.ExecuteNonQueryAsync(cancellationToken);

                    result.Success = rowsAffected > 0;
                    result.Message = result.Success 
                        ? $"Importance updated: {currentImportance:F3} → {newImportance:F3}" 
                        : "Atom not found in TensorAtoms";
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Message = ex.Message;
                    _logger.LogWarning(ex, "Failed to update importance for atom {AtomId}", update.AtomId);
                }

                results.Add(result);
            }

            var successCount = results.Count(r => r.Success);

            _logger.LogInformation("Importance update completed: {Success}/{Total} successful",
                successCount, results.Count);

            return Ok(ApiResponse<UpdateImportanceResponse>.Ok(new UpdateImportanceResponse
            {
                UpdatedCount = successCount,
                Results = results
            }, new ApiMetadata
            {
                TotalCount = results.Count,
                Extra = new Dictionary<string, object>
                {
                    ["successRate"] = results.Count > 0 ? (double)successCount / results.Count : 0,
                    ["reason"] = request.Reason ?? "manual_update"
                }
            }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error updating importance scores");
            return StatusCode(500, ApiResponse<object>.Fail("DATABASE_ERROR", "Failed to update importance", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update importance");
            return StatusCode(500, ApiResponse<object>.Fail("UPDATE_FAILED", ex.Message));
        }
    }

    [HttpPost("fine-tune/trigger")]
    [ProducesResponseType(typeof(ApiResponse<TriggerFineTuningResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> TriggerFineTuning(
        [FromBody] TriggerFineTuningRequest request,
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

            // Get feedback samples
            var feedbackQuery = @"
                SELECT TOP (@Limit)
                    f.InferenceId,
                    f.Rating,
                    ir.InputData,
                    ir.OutputData
                FROM dbo.InferenceFeedback f
                INNER JOIN dbo.InferenceRequests ir ON ir.InferenceId = f.InferenceId
                INNER JOIN (
                    SELECT DISTINCT InferenceId
                    FROM dbo.InferenceSteps
                    WHERE ModelId = @ModelId
                ) s ON s.InferenceId = f.InferenceId
                WHERE (@StartDate IS NULL OR f.SubmittedAt >= @StartDate)
                    AND (@EndDate IS NULL OR f.SubmittedAt <= @EndDate)
                ORDER BY f.SubmittedAt DESC";

            int feedbackCount = 0;
            await using (var command = new SqlCommand(feedbackQuery, connection))
            {
                command.Parameters.Add("@ModelId", SqlDbType.UniqueIdentifier).Value = request.ModelId;
                command.Parameters.Add("@Limit", SqlDbType.Int).Value = request.FeedbackLimit ?? 1000;
                command.Parameters.Add("@StartDate", SqlDbType.DateTime2).Value = request.StartDate ?? (object)DBNull.Value;
                command.Parameters.Add("@EndDate", SqlDbType.DateTime2).Value = request.EndDate ?? (object)DBNull.Value;

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    feedbackCount++;
                }
            }

            // Create fine-tuning job record
            var insertJobQuery = @"
                INSERT INTO dbo.FineTuningJobs (ModelId, FeedbackSamplesUsed, LearningRate, Epochs, Status, StartedAt)
                OUTPUT INSERTED.JobId
                VALUES (@ModelId, @SamplesUsed, @LearningRate, @Epochs, 'pending', GETUTCDATE())";

            long jobId;
            await using (var command = new SqlCommand(insertJobQuery, connection))
            {
                command.Parameters.Add("@ModelId", SqlDbType.UniqueIdentifier).Value = request.ModelId;
                command.Parameters.Add("@SamplesUsed", SqlDbType.Int).Value = feedbackCount;
                command.Parameters.Add("@LearningRate", SqlDbType.Float).Value = request.LearningRate;
                command.Parameters.Add("@Epochs", SqlDbType.Int).Value = request.Epochs;

                jobId = (long)await command.ExecuteScalarAsync(cancellationToken);
            }

            _logger.LogInformation("Fine-tuning job {JobId} created for model {ModelId} with {Samples} feedback samples",
                jobId, request.ModelId, feedbackCount);

            return Ok(ApiResponse<TriggerFineTuningResponse>.Ok(new TriggerFineTuningResponse
            {
                FineTuningJobId = jobId,
                Status = "pending",
                FeedbackSamplesUsed = feedbackCount,
                StartedAt = DateTime.UtcNow,
                Message = $"Fine-tuning job queued with {feedbackCount} feedback samples"
            }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error triggering fine-tuning");
            return StatusCode(500, ApiResponse<object>.Fail("DATABASE_ERROR", "Failed to trigger fine-tuning", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger fine-tuning");
            return StatusCode(500, ApiResponse<object>.Fail("FINETUNING_FAILED", ex.Message));
        }
    }

    [HttpPost("summary")]
    [ProducesResponseType(typeof(ApiResponse<GetFeedbackSummaryResponse>), 200)]
    public async Task<IActionResult> GetFeedbackSummary(
        [FromBody] GetFeedbackSummaryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var query = @"
                SELECT 
                    COUNT(*) AS TotalFeedback,
                    AVG(CAST(Rating AS FLOAT)) AS AvgRating,
                    SUM(CASE WHEN Rating >= 4 THEN 1 ELSE 0 END) AS PositiveCount,
                    SUM(CASE WHEN Rating <= 2 THEN 1 ELSE 0 END) AS NegativeCount
                FROM dbo.InferenceFeedback f
                LEFT JOIN dbo.InferenceSteps s ON s.InferenceId = f.InferenceId
                WHERE (@ModelId IS NULL OR s.ModelId = @ModelId)
                    AND (@StartDate IS NULL OR f.SubmittedAt >= @StartDate)
                    AND (@EndDate IS NULL OR f.SubmittedAt <= @EndDate);

                -- Rating distribution
                SELECT Rating, COUNT(*) AS Count
                FROM dbo.InferenceFeedback f
                LEFT JOIN dbo.InferenceSteps s ON s.InferenceId = f.InferenceId
                WHERE (@ModelId IS NULL OR s.ModelId = @ModelId)
                    AND (@StartDate IS NULL OR f.SubmittedAt >= @StartDate)
                    AND (@EndDate IS NULL OR f.SubmittedAt <= @EndDate)
                GROUP BY Rating;

                -- Trends (daily)
                SELECT 
                    CAST(f.SubmittedAt AS DATE) AS FeedbackDate,
                    COUNT(*) AS DailyCount,
                    AVG(CAST(Rating AS FLOAT)) AS DailyAvgRating
                FROM dbo.InferenceFeedback f
                LEFT JOIN dbo.InferenceSteps s ON s.InferenceId = f.InferenceId
                WHERE (@ModelId IS NULL OR s.ModelId = @ModelId)
                    AND (@StartDate IS NULL OR f.SubmittedAt >= @StartDate)
                    AND (@EndDate IS NULL OR f.SubmittedAt <= @EndDate)
                GROUP BY CAST(f.SubmittedAt AS DATE)
                ORDER BY FeedbackDate DESC;";

            await using var command = new SqlCommand(query, connection);
            command.Parameters.Add("@ModelId", SqlDbType.UniqueIdentifier).Value = request?.ModelId ?? (object)DBNull.Value;
            command.Parameters.Add("@StartDate", SqlDbType.DateTime2).Value = request?.StartDate ?? (object)DBNull.Value;
            command.Parameters.Add("@EndDate", SqlDbType.DateTime2).Value = request?.EndDate ?? (object)DBNull.Value;

            var response = new GetFeedbackSummaryResponse
            {
                RatingDistribution = new Dictionary<int, long>(),
                Trends = new List<FeedbackTrendPoint>()
            };

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            // Read summary
            if (await reader.ReadAsync(cancellationToken))
            {
                response.TotalFeedback = reader.GetInt32(0);
                response.AverageRating = reader.GetDoubleOrNull(1) ?? 0;
                response.PositiveFeedbackCount = reader.GetInt32(2);
                response.NegativeFeedbackCount = reader.GetInt32(3);
            }

            // Read rating distribution
            if (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    response.RatingDistribution[reader.GetInt32(0)] = reader.GetInt32(1);
                }
            }

            // Read trends
            if (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    response.Trends.Add(new FeedbackTrendPoint
                    {
                        Date = reader.GetDateTime(0),
                        FeedbackCount = reader.GetInt32(1),
                        AverageRating = reader.GetDouble(2)
                    });
                }
            }

            _logger.LogInformation("Feedback summary retrieved: {Total} total, {AvgRating:F2} avg rating",
                response.TotalFeedback, response.AverageRating);

            return Ok(ApiResponse<GetFeedbackSummaryResponse>.Ok(response));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error retrieving feedback summary");
            return StatusCode(500, ApiResponse<object>.Fail("DATABASE_ERROR", "Failed to retrieve feedback summary", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve feedback summary");
            return StatusCode(500, ApiResponse<object>.Fail("SUMMARY_FAILED", ex.Message));
        }
    }

    private async Task UpdateAtomImportance(SqlConnection connection, List<long> atomIds, double delta, CancellationToken cancellationToken)
    {
        if (atomIds == null || atomIds.Count == 0) return;

        var atomIdList = string.Join(",", atomIds);
        var query = $@"
            UPDATE dbo.TensorAtoms
            SET ImportanceScore = CASE 
                WHEN ImportanceScore + @Delta > 1.0 THEN 1.0
                WHEN ImportanceScore + @Delta < 0.0 THEN 0.0
                ELSE ImportanceScore + @Delta
            END,
            LastModified = GETUTCDATE()
            WHERE AtomId IN ({atomIdList})";

        await using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@Delta", SqlDbType.Float).Value = delta;

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Updated importance for {Count} atoms with delta {Delta:F3}",
            atomIds.Count, delta);
    }
}

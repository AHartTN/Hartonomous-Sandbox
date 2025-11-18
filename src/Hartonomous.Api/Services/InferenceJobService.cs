using Hartonomous.Core.Interfaces;
using Hartonomous.Api.DTOs.Inference;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace Hartonomous.Api.Services;

/// <summary>
/// Service for managing inference jobs via T-SQL procedures
/// </summary>
public class InferenceJobService
{
    private readonly ISqlCommandExecutor _sqlExecutor;
    private readonly ILogger<InferenceJobService> _logger;

    public InferenceJobService(
        ISqlCommandExecutor sqlExecutor,
        ILogger<InferenceJobService> logger)
    {
        _sqlExecutor = sqlExecutor ?? throw new ArgumentNullException(nameof(sqlExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Submits a new inference job
    /// </summary>
    public async Task<JobSubmittedResponse> SubmitJobAsync(
        string taskType,
        object inputData,
        CancellationToken cancellationToken = default)
    {
        var inputDataJson = JsonSerializer.Serialize(inputData);

        return await _sqlExecutor.ExecuteAsync(async (command, ct) =>
        {
            command.CommandText = "dbo.sp_SubmitInferenceJob";
            command.CommandType = System.Data.CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@taskType", taskType);
            command.Parameters.AddWithValue("@inputData", inputDataJson);

            var correlationIdParam = new SqlParameter("@correlationId", System.Data.SqlDbType.NVarChar, 100)
            {
                Direction = System.Data.ParameterDirection.Output
            };
            command.Parameters.Add(correlationIdParam);

            var inferenceIdParam = new SqlParameter("@inferenceId", System.Data.SqlDbType.BigInt)
            {
                Direction = System.Data.ParameterDirection.Output
            };
            command.Parameters.Add(inferenceIdParam);

            await command.ExecuteNonQueryAsync(ct);

            var inferenceId = (long)inferenceIdParam.Value;
            var correlationId = (string)correlationIdParam.Value;

            return new JobSubmittedResponse
            {
                JobId = inferenceId,
                Status = "Pending",
                StatusUrl = $"/api/inference/jobs/{inferenceId}"
            };
        }, cancellationToken);
    }

    /// <summary>
    /// Gets the status of an inference job
    /// </summary>
    public async Task<JobStatusResponse?> GetJobStatusAsync(
        long jobId,
        CancellationToken cancellationToken = default)
    {
        return await _sqlExecutor.ExecuteAsync(async (command, ct) =>
        {
            command.CommandText = "dbo.sp_GetInferenceJobStatus";
            command.CommandType = System.Data.CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@inferenceId", jobId);

            using var reader = await command.ExecuteReaderAsync(ct);

            if (!await reader.ReadAsync(ct))
            {
                return null;
            }

            return new JobStatusResponse
            {
                JobId = reader.GetInt64(0),
                TaskType = reader.IsDBNull(1) ? null : reader.GetString(1),
                Status = reader.IsDBNull(2) ? "Unknown" : reader.GetString(2),
                OutputData = reader.IsDBNull(3) ? null : reader.GetString(3),
                Confidence = reader.IsDBNull(4) ? null : (double?)reader.GetDecimal(4),
                DurationMs = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                CreatedAt = reader.GetDateTime(6)
            };
        }, cancellationToken);
    }

    /// <summary>
    /// Updates the status of an inference job
    /// </summary>
    public async Task UpdateJobStatusAsync(
        long jobId,
        string status,
        string? outputData = null,
        double? confidence = null,
        int? durationMs = null,
        CancellationToken cancellationToken = default)
    {
        await _sqlExecutor.ExecuteAsync(async (command, ct) =>
        {
            command.CommandText = "dbo.sp_UpdateInferenceJobStatus";
            command.CommandType = System.Data.CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@inferenceId", jobId);
            command.Parameters.AddWithValue("@status", status);

            if (outputData != null)
                command.Parameters.AddWithValue("@outputData", outputData);

            if (confidence.HasValue)
                command.Parameters.AddWithValue("@confidence", (decimal)confidence.Value);

            if (durationMs.HasValue)
                command.Parameters.AddWithValue("@totalDurationMs", durationMs.Value);

            await command.ExecuteNonQueryAsync(ct);
        }, cancellationToken);
    }
}

using Azure.Core;
using Azure.Identity;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Inference;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.Inference;

/// <summary>
/// SQL Server implementation of inference operations.
/// Manages inference job queue, model scoring, and ensemble operations.
/// </summary>
public sealed class SqlInferenceService : IInferenceService
{
    private readonly string _connectionString;
    private readonly TokenCredential _credential;
    private readonly ILogger<SqlInferenceService> _logger;

    public SqlInferenceService(
        ILogger<SqlInferenceService> logger,
        IOptions<DatabaseOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var databaseOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectionString = databaseOptions.HartonomousDb;
        _credential = new DefaultAzureCredential();
    }

    public async Task<long> SubmitJobAsync(
        int modelId,
        string inputData,
        int priority = 5,
        int tenantId = 0,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputData);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(modelId);

        _logger.LogInformation("SubmitJob: ModelId {ModelId}, Priority {Priority}", modelId, priority);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_SubmitInferenceJob", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 30
        };

        command.Parameters.AddWithValue("@modelId", modelId);
        command.Parameters.AddWithValue("@inputData", inputData);
        command.Parameters.AddWithValue("@priority", priority);
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@correlationId", correlationId ?? (object)DBNull.Value);

        var inferenceIdParam = new SqlParameter("@inferenceId", SqlDbType.BigInt) 
        { Direction = ParameterDirection.Output };
        command.Parameters.Add(inferenceIdParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var inferenceId = (long)(inferenceIdParam.Value ?? 0L);
        _logger.LogInformation("SubmitJob completed: InferenceId {InferenceId}", inferenceId);

        return inferenceId;
    }

    public async Task<JobStatus> GetJobStatusAsync(
        long inferenceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inferenceId);

        _logger.LogInformation("GetJobStatus: InferenceId {InferenceId}", inferenceId);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_GetInferenceJobStatus", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 30
        };

        command.Parameters.AddWithValue("@inferenceId", inferenceId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return new JobStatus(
                reader.GetInt64(reader.GetOrdinal("InferenceId")),
                reader.GetString(reader.GetOrdinal("Status")),
                reader.IsDBNull(reader.GetOrdinal("OutputData")) ? null : reader.GetString(reader.GetOrdinal("OutputData")),
                reader.IsDBNull(reader.GetOrdinal("ErrorMessage")) ? null : reader.GetString(reader.GetOrdinal("ErrorMessage")),
                reader.IsDBNull(reader.GetOrdinal("CompletedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedAt")));
        }

        throw new InvalidOperationException($"Inference job {inferenceId} not found");
    }

    public async Task UpdateJobStatusAsync(
        long inferenceId,
        string status,
        string? outputData = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inferenceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        _logger.LogInformation("UpdateJobStatus: InferenceId {InferenceId}, Status {Status}", 
            inferenceId, status);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_UpdateInferenceJobStatus", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 30
        };

        command.Parameters.AddWithValue("@inferenceId", inferenceId);
        command.Parameters.AddWithValue("@status", status);
        command.Parameters.AddWithValue("@outputData", outputData ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@errorMessage", errorMessage ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("UpdateJobStatus completed for InferenceId {InferenceId}", inferenceId);
    }

    public async Task<InferenceResult> RunAsync(
        int modelId,
        string inputData,
        int tenantId = 0,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(modelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputData);

        _logger.LogInformation("RunInference: ModelId {ModelId}", modelId);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_RunInference", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 180 // 3 minutes for inference
        };

        command.Parameters.AddWithValue("@contextAtomIds", inputData);
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@temperature", 1.0f);
        command.Parameters.AddWithValue("@topK", 10);
        command.Parameters.AddWithValue("@topP", 0.9f);
        command.Parameters.AddWithValue("@maxTokens", 100);

        var inferenceIdParam = new SqlParameter("@inferenceId", SqlDbType.BigInt) 
        { Direction = ParameterDirection.Output };
        command.Parameters.Add(inferenceIdParam);

        var startTime = DateTime.UtcNow;
        
        // Execute and read results
        var outputTokens = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            if (!reader.IsDBNull(reader.GetOrdinal("CanonicalText")))
            {
                outputTokens.Add(reader.GetString(reader.GetOrdinal("CanonicalText")));
            }
        }

        var durationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
        var inferenceId = (long)(inferenceIdParam.Value ?? 0L);
        var outputData = string.Join(" ", outputTokens);

        _logger.LogInformation("RunInference completed: InferenceId {InferenceId}, Duration {Duration}ms", 
            inferenceId, durationMs);

        return new InferenceResult(inferenceId, outputData, durationMs, null);
    }

    public async Task<ScoreResult> ScoreAsync(
        int modelId,
        long atomId,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(modelId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(atomId);

        _logger.LogInformation("Score: ModelId {ModelId}, AtomId {AtomId}", modelId, atomId);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_ScoreWithModel", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@modelId", modelId);
        command.Parameters.AddWithValue("@atomId", atomId);
        command.Parameters.AddWithValue("@tenantId", tenantId);

        var scoreParam = new SqlParameter("@score", SqlDbType.Float) 
        { Direction = ParameterDirection.Output };
        var explanationParam = new SqlParameter("@explanation", SqlDbType.NVarChar, -1) 
        { Direction = ParameterDirection.Output };

        command.Parameters.Add(scoreParam);
        command.Parameters.Add(explanationParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var score = (float)(double)(scoreParam.Value ?? 0.0);
        var explanation = explanationParam.Value?.ToString();

        _logger.LogInformation("Score completed: Score {Score}", score);

        return new ScoreResult(atomId, modelId, score, explanation);
    }

    public async Task<EnsembleResult> EnsembleAsync(
        string modelIds,
        string ensembleType = "voting",
        string inputData = "",
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelIds);

        _logger.LogInformation("Ensemble: Models {Models}, Type {Type}", modelIds, ensembleType);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_MultiModelEnsemble", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 180
        };

        command.Parameters.AddWithValue("@modelIds", modelIds);
        command.Parameters.AddWithValue("@ensembleType", ensembleType);
        command.Parameters.AddWithValue("@inputData", inputData);
        command.Parameters.AddWithValue("@tenantId", tenantId);

        var predictions = new List<ModelPrediction>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            predictions.Add(new ModelPrediction(
                reader.GetInt32(reader.GetOrdinal("ModelId")),
                reader.GetString(reader.GetOrdinal("Prediction")),
                (float)reader.GetDouble(reader.GetOrdinal("Confidence"))));
        }

        // Determine combined prediction based on ensemble type
        var combinedPrediction = ensembleType.ToLowerInvariant() switch
        {
            "voting" => predictions.GroupBy(p => p.Prediction)
                                 .OrderByDescending(g => g.Count())
                                 .First().Key,
            "averaging" => predictions.OrderByDescending(p => p.Confidence)
                                    .First().Prediction,
            _ => predictions.First().Prediction
        };

        _logger.LogInformation("Ensemble completed: {PredictionCount} predictions", predictions.Count);

        return new EnsembleResult(combinedPrediction, predictions);
    }

    public async Task<ComparisonResult> CompareModelsAsync(
        int model1Id,
        int model2Id,
        int topK = 20,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(model1Id);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(model2Id);

        _logger.LogInformation("CompareModels: Model1 {Model1}, Model2 {Model2}, TopK {TopK}", 
            model1Id, model2Id, topK);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_CompareModelKnowledge", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        command.Parameters.AddWithValue("@model1Id", model1Id);
        command.Parameters.AddWithValue("@model2Id", model2Id);
        command.Parameters.AddWithValue("@topK", topK);

        var similarityParam = new SqlParameter("@similarityScore", SqlDbType.Float) 
        { Direction = ParameterDirection.Output };
        command.Parameters.Add(similarityParam);

        var differences = new List<KnowledgeDifference>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            differences.Add(new KnowledgeDifference(
                reader.GetString(reader.GetOrdinal("Dimension")),
                (float)reader.GetDouble(reader.GetOrdinal("Model1Value")),
                (float)reader.GetDouble(reader.GetOrdinal("Model2Value")),
                (float)reader.GetDouble(reader.GetOrdinal("Difference"))));
        }

        var similarityScore = (float)(double)(similarityParam.Value ?? 0.0);

        _logger.LogInformation("CompareModels completed: Similarity {Similarity}", similarityScore);

        return new ComparisonResult(model1Id, model2Id, similarityScore, differences);
    }

    public async Task<IEnumerable<InferenceHistoryItem>> GetHistoryAsync(
        long? sessionId = null,
        int? modelId = null,
        int limitRows = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limitRows);

        _logger.LogInformation("GetHistory: SessionId {SessionId}, ModelId {ModelId}, Limit {Limit}", 
            sessionId, modelId, limitRows);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_InferenceHistory", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@sessionId", sessionId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@modelId", modelId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@limitRows", limitRows);

        var history = new List<InferenceHistoryItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            history.Add(new InferenceHistoryItem(
                reader.GetInt64(reader.GetOrdinal("InferenceId")),
                reader.IsDBNull(reader.GetOrdinal("SessionId")) ? null : reader.GetInt64(reader.GetOrdinal("SessionId")),
                reader.GetInt32(reader.GetOrdinal("ModelId")),
                reader.GetString(reader.GetOrdinal("InputData")),
                reader.IsDBNull(reader.GetOrdinal("OutputData")) ? null : reader.GetString(reader.GetOrdinal("OutputData")),
                reader.GetString(reader.GetOrdinal("Status")),
                reader.GetDateTime(reader.GetOrdinal("CreatedAt"))));
        }

        _logger.LogInformation("GetHistory completed: {HistoryCount} items", history.Count);

        return history;
    }

    private async Task SetupConnectionAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        // Use managed identity if no password in connection string
        if (!_connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase) &&
            !_connectionString.Contains("Integrated Security=true", StringComparison.OrdinalIgnoreCase))
        {
            var tokenRequestContext = new TokenRequestContext(["https://database.windows.net/.default"]);
            var token = await _credential.GetTokenAsync(tokenRequestContext, cancellationToken);
            connection.AccessToken = token.Token;
        }

        await connection.OpenAsync(cancellationToken);
    }
}

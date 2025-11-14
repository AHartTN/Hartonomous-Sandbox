using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.ValueObjects;
using Hartonomous.Data;
using Hartonomous.Infrastructure.Data.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Services.Inference;

/// <summary>
/// Implements ensemble inference using SQL Server stored procedures and EF Core for metadata retrieval.
/// Combines predictions from multiple models to improve accuracy through consensus and weighted voting.
/// </summary>
public sealed class EnsembleInferenceService : IEnsembleInferenceService
{
    private readonly HartonomousDbContext _context;
    private readonly ILogger<EnsembleInferenceService> _logger;

    /// <summary>
    /// Initializes a new ensemble inference service.
    /// </summary>
    /// <param name="context">Database context for EF Core queries.</param>
    /// <param name="logger">Structured logger for diagnostics.</param>
    public EnsembleInferenceService(
        HartonomousDbContext context,
        ILogger<EnsembleInferenceService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<EnsembleInferenceResult> EnsembleInferenceAsync(
        string inputData,
        IReadOnlyList<int> modelIds,
        IReadOnlyList<float>? weights = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Executing ensemble inference with {ModelCount} models",
            modelIds.Count);

        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        long inferenceId = 0;
        var resultRows = new List<EnsembleAtomScore>();

        // Step 1: Execute sp_EnsembleInference
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "dbo.sp_EnsembleInference";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter("@inputData", inputData));
            command.Parameters.Add(new SqlParameter("@modelIds", string.Join(",", modelIds)));
            command.Parameters.Add(new SqlParameter("@taskType", "classification"));

            using var reader = (SqlDataReader)await command.ExecuteReaderAsync(cancellationToken);

            var infIdOrd = reader.GetOrdinal("InferenceId");
            var embIdOrd = reader.GetOrdinal("AtomEmbeddingId");
            var atomIdOrd = reader.GetOrdinal("AtomId");
            var textOrd = reader.GetOrdinal("CanonicalText");
            var countOrd = reader.GetOrdinal("ModelCount");
            var scoreOrd = reader.GetOrdinal("EnsembleScore");
            var consOrd = reader.GetOrdinal("IsConsensus");

            while (await reader.ReadAsync(cancellationToken))
            {
                // First row contains InferenceId
                if (inferenceId == 0)
                    inferenceId = reader.GetInt64(infIdOrd);

                resultRows.Add(new EnsembleAtomScore
                {
                    AtomEmbeddingId = reader.GetInt64(embIdOrd),
                    AtomId = reader.GetInt64(atomIdOrd),
                    CanonicalText = reader.GetStringOrNull(textOrd) ?? string.Empty,
                    ModelCount = reader.GetInt32(countOrd),
                    EnsembleScore = reader.GetDouble(scoreOrd),
                    IsConsensus = reader.GetInt32(consOrd) == 1
                });
            }
        }

        // Step 2: Query InferenceRequests table to get metadata and steps
        var inferenceRequest = await _context.InferenceRequests
            .Include(i => i.Steps)
                .ThenInclude(s => s.Model)
            .FirstOrDefaultAsync(i => i.InferenceId == inferenceId, cancellationToken);

        if (inferenceRequest == null)
        {
            _logger.LogWarning("InferenceRequest {InferenceId} not found after ensemble execution", inferenceId);
            return new EnsembleInferenceResult
            {
                InferenceId = inferenceId,
                OutputData = JsonSerializer.Serialize(resultRows),
                ConfidenceScore = 0.0f,
                ModelContributions = [],
                CompletedTimestamp = DateTime.UtcNow
            };
        }

        // Step 3: Calculate confidence from consensus
        var totalModels = inferenceRequest.Steps.Count;
        var consensusCount = resultRows.Count(r => r.IsConsensus);
        var confidence = totalModels > 0 && resultRows.Count > 0
            ? (float)consensusCount / resultRows.Count
            : 0.0f;

        // Step 4: Parse weights from ModelsUsed JSON
        var modelWeights = new Dictionary<int, float>();
        if (!string.IsNullOrEmpty(inferenceRequest.ModelsUsed))
        {
            try
            {
                using var doc = JsonDocument.Parse(inferenceRequest.ModelsUsed);
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("ModelId", out var modelIdProp) &&
                        modelIdProp.TryGetInt32(out var modelId))
                    {
                        var weight = element.TryGetProperty("Weight", out var weightProp) && weightProp.TryGetSingle(out var w)
                            ? w
                            : 1.0f / totalModels; // Default to equal weight if not specified
                        modelWeights[modelId] = weight;
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse ModelsUsed JSON for InferenceId={InferenceId}, using equal weights", inferenceId);
            }
        }

        // Step 5: Build ModelContributions from InferenceSteps
        var contributions = inferenceRequest.Steps
            .Where(s => s.Model != null)
            .Select(step => new ModelContribution
            {
                ModelId = step.ModelId ?? 0,
                ModelName = step.Model?.ModelName ?? "Unknown",
                IndividualOutput = $"Step {step.StepNumber}: {step.OperationType}",
                Weight = modelWeights.TryGetValue(step.ModelId ?? 0, out var weight) ? weight : 1.0f / totalModels,
                ConfidenceScore = step.DurationMs.HasValue && inferenceRequest.TotalDurationMs.HasValue
                    ? 1.0f - ((float)step.DurationMs.Value / inferenceRequest.TotalDurationMs.Value)
                    : 0.5f
            })
            .ToList();

        _logger.LogInformation(
            "Ensemble inference completed: InferenceId={InferenceId}, Confidence={Confidence:F2}, Results={ResultCount}",
            inferenceId, confidence, resultRows.Count);

        return new EnsembleInferenceResult
        {
            InferenceId = inferenceId,
            OutputData = JsonSerializer.Serialize(resultRows.Take(10)), // Top 10 results
            ConfidenceScore = confidence,
            ModelContributions = contributions,
            CompletedTimestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Helper class for parsing ensemble results from stored procedure.
    /// </summary>
    private sealed class EnsembleAtomScore
    {
        public long AtomEmbeddingId { get; init; }
        public long AtomId { get; init; }
        public string CanonicalText { get; init; } = string.Empty;
        public int ModelCount { get; init; }
        public double EnsembleScore { get; init; }
        public bool IsConsensus { get; init; }
    }
}

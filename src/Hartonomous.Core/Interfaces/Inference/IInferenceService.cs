using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces.Inference;

/// <summary>
/// Service for inference operations including job submission and model execution.
/// </summary>
public interface IInferenceService
{
    /// <summary>
    /// Submits async inference job to queue.
    /// Calls sp_SubmitInferenceJob stored procedure.
    /// </summary>
    Task<long> SubmitJobAsync(
        int modelId,
        string inputData,
        int priority = 5,
        int tenantId = 0,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves inference job status and output.
    /// Calls sp_GetInferenceJobStatus stored procedure.
    /// </summary>
    Task<JobStatus> GetJobStatusAsync(
        long inferenceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates inference job status and results.
    /// Calls sp_UpdateInferenceJobStatus stored procedure.
    /// </summary>
    Task UpdateJobStatusAsync(
        long inferenceId,
        string status,
        string? outputData = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes ML inference with telemetry.
    /// Calls sp_RunInference stored procedure.
    /// </summary>
    Task<InferenceResult> RunAsync(
        int modelId,
        string inputData,
        int tenantId = 0,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scores atom using specified model.
    /// Calls sp_ScoreWithModel stored procedure.
    /// </summary>
    Task<ScoreResult> ScoreAsync(
        int modelId,
        long atomId,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensembles predictions from multiple models.
    /// Calls sp_MultiModelEnsemble stored procedure.
    /// </summary>
    Task<EnsembleResult> EnsembleAsync(
        string modelIds,
        string ensembleType = "voting",
        string inputData = "",
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares knowledge (embeddings) between two models.
    /// Calls sp_CompareModelKnowledge stored procedure.
    /// </summary>
    Task<ComparisonResult> CompareModelsAsync(
        int model1Id,
        int model2Id,
        int topK = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves inference request history with filtering.
    /// Calls sp_InferenceHistory stored procedure.
    /// </summary>
    Task<IEnumerable<InferenceHistoryItem>> GetHistoryAsync(
        long? sessionId = null,
        int? modelId = null,
        int limitRows = 100,
        CancellationToken cancellationToken = default);
}

public record JobStatus(
    long InferenceId,
    string Status,
    string? OutputData,
    string? ErrorMessage,
    DateTime? CompletedAt);

public record InferenceResult(
    long InferenceId,
    string OutputData,
    int DurationMs,
    float? Confidence);

public record ScoreResult(
    long AtomId,
    int ModelId,
    float Score,
    string? Explanation);

public record EnsembleResult(
    string CombinedPrediction,
    IEnumerable<ModelPrediction> IndividualPredictions);

public record ModelPrediction(
    int ModelId,
    string Prediction,
    float Confidence);

public record ComparisonResult(
    int Model1Id,
    int Model2Id,
    float SimilarityScore,
    IEnumerable<KnowledgeDifference> Differences);

public record KnowledgeDifference(
    string Dimension,
    float Model1Value,
    float Model2Value,
    float Difference);

public record InferenceHistoryItem(
    long InferenceId,
    long? SessionId,
    int ModelId,
    string InputData,
    string? OutputData,
    string Status,
    DateTime CreatedAt);

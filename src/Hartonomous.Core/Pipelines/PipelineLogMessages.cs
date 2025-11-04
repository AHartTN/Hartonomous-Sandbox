using System;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Pipelines;

/// <summary>
/// High-performance logger messages using source generation.
/// 10-100x faster than ILogger.LogInformation() due to eliminated boxing and string interpolation.
/// </summary>
public static partial class PipelineLogMessages
{
    // ============================================================
    // Pipeline Lifecycle
    // ============================================================

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Pipeline '{PipelineName}' starting. CorrelationId: {CorrelationId}, InputType: {InputType}")]
    public static partial void LogPipelineStarting(
        this ILogger logger,
        string pipelineName,
        string correlationId,
        string inputType);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Pipeline '{PipelineName}' completed successfully. CorrelationId: {CorrelationId}, Duration: {DurationMs}ms, StepCount: {StepCount}")]
    public static partial void LogPipelineCompleted(
        this ILogger logger,
        string pipelineName,
        string correlationId,
        double durationMs,
        int stepCount);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "Pipeline '{PipelineName}' failed. CorrelationId: {CorrelationId}, FailedStep: {FailedStep}, ErrorCode: {ErrorCode}, Duration: {DurationMs}ms")]
    public static partial void LogPipelineFailed(
        this ILogger logger,
        string pipelineName,
        string correlationId,
        string failedStep,
        string errorCode,
        double durationMs,
        Exception? exception);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Pipeline '{PipelineName}' validation failed. CorrelationId: {CorrelationId}, ValidationErrors: {ErrorCount}")]
    public static partial void LogPipelineValidationFailed(
        this ILogger logger,
        string pipelineName,
        string correlationId,
        int errorCount);

    // ============================================================
    // Pipeline Step Execution
    // ============================================================

    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Debug,
        Message = "Step '{StepName}' starting. CorrelationId: {CorrelationId}, InputType: {InputType}, OutputType: {OutputType}")]
    public static partial void LogStepStarting(
        this ILogger logger,
        string stepName,
        string correlationId,
        string inputType,
        string outputType);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Debug,
        Message = "Step '{StepName}' completed. CorrelationId: {CorrelationId}, Duration: {DurationMs}ms")]
    public static partial void LogStepCompleted(
        this ILogger logger,
        string stepName,
        string correlationId,
        double durationMs);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Error,
        Message = "Step '{StepName}' failed. CorrelationId: {CorrelationId}, ErrorCode: {ErrorCode}, IsRetryable: {IsRetryable}, Duration: {DurationMs}ms")]
    public static partial void LogStepFailed(
        this ILogger logger,
        string stepName,
        string correlationId,
        string errorCode,
        bool isRetryable,
        double durationMs,
        Exception? exception);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Warning,
        Message = "Step '{StepName}' skipped. CorrelationId: {CorrelationId}, Reason: {Reason}")]
    public static partial void LogStepSkipped(
        this ILogger logger,
        string stepName,
        string correlationId,
        string reason);

    // ============================================================
    // Resilience (Polly Integration)
    // ============================================================

    [LoggerMessage(
        EventId = 3000,
        Level = LogLevel.Warning,
        Message = "Retry attempt {AttemptNumber} for pipeline '{PipelineName}' step '{StepName}'. CorrelationId: {CorrelationId}, Delay: {DelayMs}ms")]
    public static partial void LogRetryAttempt(
        this ILogger logger,
        int attemptNumber,
        string pipelineName,
        string stepName,
        string correlationId,
        double delayMs);

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Error,
        Message = "Circuit breaker opened for pipeline '{PipelineName}'. CorrelationId: {CorrelationId}, FailureRatio: {FailureRatio:F2}, BreakDuration: {BreakDurationSec}s")]
    public static partial void LogCircuitBreakerOpened(
        this ILogger logger,
        string pipelineName,
        string correlationId,
        double failureRatio,
        double breakDurationSec);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Information,
        Message = "Circuit breaker closed for pipeline '{PipelineName}'. CorrelationId: {CorrelationId}")]
    public static partial void LogCircuitBreakerClosed(
        this ILogger logger,
        string pipelineName,
        string correlationId);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Warning,
        Message = "Pipeline '{PipelineName}' step '{StepName}' timed out. CorrelationId: {CorrelationId}, Timeout: {TimeoutMs}ms")]
    public static partial void LogStepTimeout(
        this ILogger logger,
        string pipelineName,
        string stepName,
        string correlationId,
        double timeoutMs);

    // ============================================================
    // Atom Ingestion Specific
    // ============================================================

    [LoggerMessage(
        EventId = 4000,
        Level = LogLevel.Debug,
        Message = "Computing content hash for atom. Modality: {Modality}, InputLength: {InputLength}, CorrelationId: {CorrelationId}")]
    public static partial void LogComputingContentHash(
        this ILogger logger,
        string modality,
        int inputLength,
        string correlationId);

    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Information,
        Message = "Exact duplicate found for atom. AtomId: {AtomId}, ReferenceCount: {ReferenceCount}, CorrelationId: {CorrelationId}")]
    public static partial void LogExactDuplicateFound(
        this ILogger logger,
        long atomId,
        int referenceCount,
        string correlationId);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Information,
        Message = "Semantic duplicate found for atom. AtomId: {AtomId}, Similarity: {Similarity:F4}, Threshold: {Threshold:F4}, CorrelationId: {CorrelationId}")]
    public static partial void LogSemanticDuplicateFound(
        this ILogger logger,
        long atomId,
        double similarity,
        double threshold,
        string correlationId);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Debug,
        Message = "Generating embedding for atom. Modality: {Modality}, TextLength: {TextLength}, EmbeddingType: {EmbeddingType}, CorrelationId: {CorrelationId}")]
    public static partial void LogGeneratingEmbedding(
        this ILogger logger,
        string modality,
        int textLength,
        string embeddingType,
        string correlationId);

    [LoggerMessage(
        EventId = 4004,
        Level = LogLevel.Warning,
        Message = "Embedding generation failed for atom. Modality: {Modality}, ErrorMessage: {ErrorMessage}, CorrelationId: {CorrelationId}")]
    public static partial void LogEmbeddingGenerationFailed(
        this ILogger logger,
        string modality,
        string errorMessage,
        string correlationId);

    [LoggerMessage(
        EventId = 4005,
        Level = LogLevel.Information,
        Message = "Created new atom. AtomId: {AtomId}, Modality: {Modality}, HasEmbedding: {HasEmbedding}, CorrelationId: {CorrelationId}")]
    public static partial void LogAtomCreated(
        this ILogger logger,
        long atomId,
        string modality,
        bool hasEmbedding,
        string correlationId);

    // ============================================================
    // Background Worker Specific
    // ============================================================

    [LoggerMessage(
        EventId = 5000,
        Level = LogLevel.Information,
        Message = "Background worker '{WorkerName}' starting. QueueCapacity: {QueueCapacity}")]
    public static partial void LogWorkerStarting(
        this ILogger logger,
        string workerName,
        int queueCapacity);

    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Information,
        Message = "Background worker '{WorkerName}' stopping. ProcessedCount: {ProcessedCount}, FailedCount: {FailedCount}")]
    public static partial void LogWorkerStopping(
        this ILogger logger,
        string workerName,
        long processedCount,
        long failedCount);

    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Warning,
        Message = "Background worker '{WorkerName}' queue is full. QueueDepth: {QueueDepth}, Capacity: {Capacity}, FullMode: {FullMode}")]
    public static partial void LogWorkerQueueFull(
        this ILogger logger,
        string workerName,
        int queueDepth,
        int capacity,
        string fullMode);

    [LoggerMessage(
        EventId = 5003,
        Level = LogLevel.Debug,
        Message = "Request enqueued for background processing. Worker: {workerName}, Queue Depth: {queueDepth}")]
    public static partial void LogRequestEnqueued(
        this ILogger logger,
        string workerName,
        int queueDepth);

    [LoggerMessage(
        EventId = 5004,
        Level = LogLevel.Error,
        Message = "Background worker '{workerName}' encountered fatal error.")]
    public static partial void LogWorkerFatalError(
        this ILogger logger,
        string workerName,
        Exception exception);

    // ============================================================
    // Streaming Pipelines
    // ============================================================

    [LoggerMessage(
        EventId = 6000,
        Level = LogLevel.Information,
        Message = "Starting streaming pipeline '{PipelineName}'. CorrelationId: {CorrelationId}")]
    public static partial void LogStreamingPipelineStarting(
        this ILogger logger,
        string pipelineName,
        string correlationId);

    [LoggerMessage(
        EventId = 6001,
        Level = LogLevel.Information,
        Message = "Streaming pipeline '{PipelineName}' completed. CorrelationId: {CorrelationId}, ProcessedCount: {ProcessedCount}, SuccessCount: {SuccessCount}, FailureCount: {FailureCount}, Duration: {DurationMs}ms")]
    public static partial void LogStreamingPipelineCompleted(
        this ILogger logger,
        string pipelineName,
        string correlationId,
        int processedCount,
        int successCount,
        int failureCount,
        double durationMs);

    [LoggerMessage(
        EventId = 6002,
        Level = LogLevel.Debug,
        Message = "Streaming pipeline '{PipelineName}' processing item {ItemNumber}. CorrelationId: {CorrelationId}")]
    public static partial void LogStreamingItemProcessing(
        this ILogger logger,
        string pipelineName,
        int itemNumber,
        string correlationId);

    // ============================================================
    // Performance Metrics
    // ============================================================

    [LoggerMessage(
        EventId = 7000,
        Level = LogLevel.Information,
        Message = "Pipeline '{PipelineName}' performance metrics. AvgDuration: {AvgDurationMs}ms, P95Duration: {P95DurationMs}ms, P99Duration: {P99DurationMs}ms, ThroughputPerSec: {ThroughputPerSec:F2}")]
    public static partial void LogPipelinePerformanceMetrics(
        this ILogger logger,
        string pipelineName,
        double avgDurationMs,
        double p95DurationMs,
        double p99DurationMs,
        double throughputPerSec);

    [LoggerMessage(
        EventId = 7001,
        Level = LogLevel.Warning,
        Message = "Pipeline '{PipelineName}' performance degradation detected. CurrentP95: {CurrentP95Ms}ms, BaselineP95: {BaselineP95Ms}ms, Degradation: {DegradationPercent:F1}%")]
    public static partial void LogPerformanceDegradation(
        this ILogger logger,
        string pipelineName,
        double currentP95Ms,
        double baselineP95Ms,
        double degradationPercent);
}

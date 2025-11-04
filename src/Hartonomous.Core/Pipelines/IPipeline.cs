using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Pipelines;

/// <summary>
/// Represents a composable, observable pipeline that transforms input data into output data.
/// Pipelines support telemetry, resilience, and streaming via generic type parameters.
/// </summary>
/// <typeparam name="TInput">Type of data consumed by the pipeline.</typeparam>
/// <typeparam name="TOutput">Type of data produced by the pipeline.</typeparam>
public interface IPipeline<TInput, TOutput>
{
    /// <summary>
    /// Unique identifier for this pipeline instance (for correlation and tracing).
    /// </summary>
    string PipelineId { get; }

    /// <summary>
    /// Human-readable name for logging and telemetry.
    /// </summary>
    string PipelineName { get; }

    /// <summary>
    /// Executes the entire pipeline with a single input item.
    /// </summary>
    /// <param name="input">Input data to process.</param>
    /// <param name="context">Execution context with correlation tracking and telemetry.</param>
    /// <param name="cancellationToken">Token to cancel pipeline execution.</param>
    /// <returns>Result of pipeline execution.</returns>
    Task<PipelineResult<TOutput>> ExecuteAsync(
        TInput input,
        IPipelineContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the pipeline with streaming input/output for large datasets.
    /// Enables backpressure handling and memory-efficient processing.
    /// </summary>
    /// <param name="inputs">Async stream of input items.</param>
    /// <param name="context">Execution context with correlation tracking.</param>
    /// <param name="cancellationToken">Token to cancel pipeline execution.</param>
    /// <returns>Async stream of pipeline results.</returns>
    IAsyncEnumerable<PipelineResult<TOutput>> ExecuteStreamAsync(
        IAsyncEnumerable<TInput> inputs,
        IPipelineContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates pipeline configuration and dependencies before execution.
    /// </summary>
    Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a pipeline execution with success/failure tracking and telemetry.
/// </summary>
/// <typeparam name="TOutput">Type of output data.</typeparam>
public sealed record PipelineResult<TOutput>
{
    /// <summary>
    /// Indicates whether the pipeline execution succeeded.
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Output data produced by the pipeline (null if failed).
    /// </summary>
    public TOutput? Output { get; init; }

    /// <summary>
    /// Error information if execution failed.
    /// </summary>
    public PipelineError? Error { get; init; }

    /// <summary>
    /// Execution metadata for telemetry (duration, step count, retry count).
    /// </summary>
    public PipelineMetrics Metrics { get; init; } = new();

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>
    /// Creates a successful pipeline result.
    /// </summary>
    public static PipelineResult<TOutput> Success(
        TOutput output,
        PipelineMetrics metrics,
        string correlationId) =>
        new()
        {
            IsSuccess = true,
            Output = output,
            Metrics = metrics,
            CorrelationId = correlationId
        };

    /// <summary>
    /// Creates a failed pipeline result.
    /// </summary>
    public static PipelineResult<TOutput> Failure(
        PipelineError error,
        PipelineMetrics metrics,
        string correlationId) =>
        new()
        {
            IsSuccess = false,
            Error = error,
            Metrics = metrics,
            CorrelationId = correlationId
        };
}

/// <summary>
/// Error information for failed pipeline executions.
/// </summary>
public sealed record PipelineError
{
    public required string ErrorCode { get; init; }
    public required string Message { get; init; }
    public string? FailedStepName { get; init; }
    public Exception? Exception { get; init; }
    public bool IsRetryable { get; init; }
}

/// <summary>
/// Telemetry metrics collected during pipeline execution.
/// </summary>
public sealed record PipelineMetrics
{
    public TimeSpan TotalDuration { get; init; }
    public int StepsExecuted { get; init; }
    public int RetryCount { get; init; }
    public Dictionary<string, object> CustomMetrics { get; init; } = new();
}

/// <summary>
/// Validation result for pipeline configuration.
/// </summary>
public sealed record ValidationResult
{
    public required bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static ValidationResult Valid() => new() { IsValid = true };

    public static ValidationResult Invalid(params string[] errors) =>
        new() { IsValid = false, Errors = errors };
}

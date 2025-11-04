using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Pipelines;

/// <summary>
/// Represents a single transformation step within a pipeline.
/// Steps are composable, reusable, and independently testable.
/// </summary>
/// <typeparam name="TInput">Type of input data consumed by this step.</typeparam>
/// <typeparam name="TOutput">Type of output data produced by this step.</typeparam>
public interface IPipelineStep<TInput, TOutput>
{
    /// <summary>
    /// Unique name for this step (used in telemetry and error messages).
    /// </summary>
    string StepName { get; }

    /// <summary>
    /// Executes this transformation step.
    /// </summary>
    /// <param name="input">Input data from previous step or pipeline entry.</param>
    /// <param name="context">Pipeline context for correlation tracking.</param>
    /// <param name="cancellationToken">Token to cancel execution.</param>
    /// <returns>Result of step execution (success or failure).</returns>
    Task<StepResult<TOutput>> ExecuteAsync(
        TInput input,
        IPipelineContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates step configuration and dependencies.
    /// </summary>
    Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a pipeline step execution.
/// </summary>
/// <typeparam name="TOutput">Type of output data.</typeparam>
public sealed record StepResult<TOutput>
{
    public required bool IsSuccess { get; init; }
    public TOutput? Output { get; init; }
    public PipelineError? Error { get; init; }
    public TimeSpan Duration { get; init; }
    public string StepName { get; init; } = string.Empty;

    public static StepResult<TOutput> Success(TOutput output, TimeSpan duration, string stepName) =>
        new()
        {
            IsSuccess = true,
            Output = output,
            Duration = duration,
            StepName = stepName
        };

    public static StepResult<TOutput> Failure(PipelineError error, TimeSpan duration, string stepName) =>
        new()
        {
            IsSuccess = false,
            Error = error,
            Duration = duration,
            StepName = stepName
        };
}

/// <summary>
/// Abstract base class for pipeline steps with telemetry and error handling built-in.
/// </summary>
/// <typeparam name="TInput">Type of input data.</typeparam>
/// <typeparam name="TOutput">Type of output data.</typeparam>
public abstract class PipelineStepBase<TInput, TOutput> : IPipelineStep<TInput, TOutput>
{
    public abstract string StepName { get; }

    public async Task<StepResult<TOutput>> ExecuteAsync(
        TInput input,
        IPipelineContext context,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Create child activity for this step
            using var childContext = context.CreateChild(StepName);

            childContext.TraceActivity?.SetTag("step.name", StepName);
            childContext.TraceActivity?.SetTag("step.input_type", typeof(TInput).Name);
            childContext.TraceActivity?.SetTag("step.output_type", typeof(TOutput).Name);

            // Execute the actual step logic
            var output = await ExecuteStepAsync(input, childContext, cancellationToken);

            var duration = DateTime.UtcNow - startTime;

            childContext.TraceActivity?.SetTag("step.duration_ms", duration.TotalMilliseconds);
            childContext.TraceActivity?.SetStatus(ActivityStatusCode.Ok);

            return StepResult<TOutput>.Success(output, duration, StepName);
        }
        catch (OperationCanceledException)
        {
            var duration = DateTime.UtcNow - startTime;
            var error = new PipelineError
            {
                ErrorCode = "STEP_CANCELLED",
                Message = $"Step {StepName} was cancelled",
                FailedStepName = StepName,
                IsRetryable = false
            };

            context.TraceActivity?.SetStatus(ActivityStatusCode.Error, "Cancelled");

            return StepResult<TOutput>.Failure(error, duration, StepName);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            var error = new PipelineError
            {
                ErrorCode = "STEP_EXECUTION_FAILED",
                Message = ex.Message,
                FailedStepName = StepName,
                Exception = ex,
                IsRetryable = IsRetryableException(ex)
            };

            context.TraceActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            context.TraceActivity?.AddException(ex);

            return StepResult<TOutput>.Failure(error, duration, StepName);
        }
    }

    public virtual Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ValidationResult.Valid());
    }

    /// <summary>
    /// Override this method to implement step-specific logic.
    /// </summary>
    protected abstract Task<TOutput> ExecuteStepAsync(
        TInput input,
        IPipelineContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Override to define which exceptions should trigger retries.
    /// </summary>
    protected virtual bool IsRetryableException(Exception exception)
    {
        // Default: retry on transient failures
        return exception is TimeoutException
            or HttpRequestException
            or TaskCanceledException
            or System.Data.Common.DbException;
    }
}

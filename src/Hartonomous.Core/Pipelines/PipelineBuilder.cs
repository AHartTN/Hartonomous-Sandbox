using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Hartonomous.Core.Pipelines;

/// <summary>
/// Fluent builder for composable, observable pipelines with built-in resilience.
/// Supports sequential step composition, branching, and parallel execution.
/// </summary>
/// <typeparam name="TInput">Type of pipeline input.</typeparam>
/// <typeparam name="TOutput">Type of pipeline output.</typeparam>
public sealed class PipelineBuilder<TInput, TOutput>
{
    private readonly List<IPipelineStep<object, object>> _steps = new();
    private readonly string _pipelineName;
    private readonly ILogger? _logger;
    private readonly ActivitySource? _activitySource;
    private ResiliencePipeline? _resiliencePipeline;

    private PipelineBuilder(string pipelineName, ILogger? logger, ActivitySource? activitySource)
    {
        _pipelineName = pipelineName;
        _logger = logger;
        _activitySource = activitySource;
    }

    /// <summary>
    /// Creates a new pipeline builder.
    /// </summary>
    /// <param name="pipelineName">Human-readable name for telemetry.</param>
    /// <param name="logger">Optional logger for pipeline execution.</param>
    /// <param name="activitySource">Optional ActivitySource for distributed tracing.</param>
    public static PipelineBuilder<TInput, TOutput> Create(
        string pipelineName,
        ILogger? logger = null,
        ActivitySource? activitySource = null)
    {
        return new PipelineBuilder<TInput, TOutput>(pipelineName, logger, activitySource);
    }

    /// <summary>
    /// Adds a transformation step to the pipeline.
    /// </summary>
    public PipelineBuilder<TInput, TNext> AddStep<TNext>(
        IPipelineStep<TOutput, TNext> step)
    {
        _steps.Add((IPipelineStep<object, object>)step);

        var nextBuilder = new PipelineBuilder<TInput, TNext>(_pipelineName, _logger, _activitySource);
        nextBuilder._steps.AddRange(_steps);
        nextBuilder._resiliencePipeline = _resiliencePipeline;

        return nextBuilder;
    }

    /// <summary>
    /// Adds a transformation step using a delegate (for simple transformations).
    /// </summary>
    public PipelineBuilder<TInput, TNext> AddStep<TNext>(
        string stepName,
        Func<TOutput, IPipelineContext, CancellationToken, Task<TNext>> stepFunc)
    {
        var step = new DelegateStep<TOutput, TNext>(stepName, stepFunc);
        return AddStep(step);
    }

    /// <summary>
    /// Adds a synchronous transformation step.
    /// </summary>
    public PipelineBuilder<TInput, TNext> AddStep<TNext>(
        string stepName,
        Func<TOutput, TNext> stepFunc)
    {
        var step = new DelegateStep<TOutput, TNext>(
            stepName,
            (input, _, _) => Task.FromResult(stepFunc(input)));

        return AddStep(step);
    }

    /// <summary>
    /// Configures resilience policies (retry, circuit breaker, timeout).
    /// </summary>
    public PipelineBuilder<TInput, TOutput> WithResilience(
        Action<ResiliencePipelineBuilder> configure)
    {
        var builder = new ResiliencePipelineBuilder();
        configure(builder);
        _resiliencePipeline = builder.Build();

        return this;
    }

    /// <summary>
    /// Configures standard resilience (retry + circuit breaker + timeout).
    /// </summary>
    public PipelineBuilder<TInput, TOutput> WithStandardResilience(
        int maxRetries = 3,
        TimeSpan? timeout = null,
        double circuitBreakerFailureThreshold = 0.5)
    {
        return WithResilience(builder =>
        {
            // Retry with exponential backoff
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = maxRetries,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger?.LogWarning(
                        "Pipeline step retry attempt {Attempt} after {Delay}ms. Exception: {Exception}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message);
                    return default;
                }
            });

            // Circuit breaker
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = circuitBreakerFailureThreshold,
                MinimumThroughput = 3,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(30),
                OnOpened = args =>
                {
                    _logger?.LogError(
                        "Circuit breaker opened for pipeline {PipelineName}. Failure ratio: {FailureRatio}",
                        _pipelineName,
                        circuitBreakerFailureThreshold);
                    return default;
                },
                OnClosed = args =>
                {
                    _logger?.LogInformation(
                        "Circuit breaker closed for pipeline {PipelineName}",
                        _pipelineName);
                    return default;
                }
            });

            // Timeout
            if (timeout.HasValue)
            {
                builder.AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = timeout.Value,
                    OnTimeout = args =>
                    {
                        _logger?.LogError(
                            "Pipeline step timeout after {Timeout}ms",
                            timeout.Value.TotalMilliseconds);
                        return default;
                    }
                });
            }
        });
    }

    /// <summary>
    /// Builds the immutable pipeline.
    /// </summary>
    public IPipeline<TInput, TOutput> Build()
    {
        return new ComposedPipeline<TInput, TOutput>(
            _pipelineName,
            _steps,
            _logger,
            _activitySource,
            _resiliencePipeline);
    }

    /// <summary>
    /// Simple delegate-based step for inline transformations.
    /// </summary>
    private sealed class DelegateStep<TIn, TOut> : PipelineStepBase<TIn, TOut>
    {
        private readonly Func<TIn, IPipelineContext, CancellationToken, Task<TOut>> _func;

        public override string StepName { get; }

        public DelegateStep(
            string stepName,
            Func<TIn, IPipelineContext, CancellationToken, Task<TOut>> func)
        {
            StepName = stepName;
            _func = func;
        }

        protected override Task<TOut> ExecuteStepAsync(
            TIn input,
            IPipelineContext context,
            CancellationToken cancellationToken)
        {
            return _func(input, context, cancellationToken);
        }
    }
}

/// <summary>
/// Composed pipeline implementation with sequential step execution.
/// </summary>
internal sealed class ComposedPipeline<TInput, TOutput> : IPipeline<TInput, TOutput>
{
    private readonly List<IPipelineStep<object, object>> _steps;
    private readonly ILogger? _logger;
    private readonly ActivitySource? _activitySource;
    private readonly ResiliencePipeline? _resiliencePipeline;

    public string PipelineId { get; } = Guid.NewGuid().ToString("N");
    public string PipelineName { get; }

    public ComposedPipeline(
        string pipelineName,
        List<IPipelineStep<object, object>> steps,
        ILogger? logger,
        ActivitySource? activitySource,
        ResiliencePipeline? resiliencePipeline)
    {
        PipelineName = pipelineName;
        _steps = steps;
        _logger = logger;
        _activitySource = activitySource;
        _resiliencePipeline = resiliencePipeline;
    }

    public async Task<PipelineResult<TOutput>> ExecuteAsync(
        TInput input,
        IPipelineContext context,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var stepCount = 0;
        var retryCount = 0;

        try
        {
            _logger?.LogInformation(
                "Pipeline {PipelineName} starting execution. CorrelationId: {CorrelationId}",
                PipelineName,
                context.CorrelationId);

            context.TraceActivity?.SetTag("pipeline.name", PipelineName);
            context.TraceActivity?.SetTag("pipeline.id", PipelineId);
            context.TraceActivity?.SetTag("pipeline.step_count", _steps.Count);

            // Execute steps sequentially
            object current = input!;
            foreach (var step in _steps)
            {
                stepCount++;

                // Apply resilience if configured
                if (_resiliencePipeline != null)
                {
                    var stepResult = await _resiliencePipeline.ExecuteAsync(
                        async ct =>
                        {
                            var result = await step.ExecuteAsync(current, context, ct);
                            if (!result.IsSuccess)
                            {
                                throw new PipelineStepException(result.Error!);
                            }
                            return result;
                        },
                        cancellationToken);

                    if (!stepResult.IsSuccess)
                    {
                        var duration = DateTime.UtcNow - startTime;
                        return PipelineResult<TOutput>.Failure(
                            stepResult.Error!,
                            new PipelineMetrics
                            {
                                TotalDuration = duration,
                                StepsExecuted = stepCount,
                                RetryCount = retryCount
                            },
                            context.CorrelationId);
                    }

                    current = stepResult.Output!;
                }
                else
                {
                    var stepResult = await step.ExecuteAsync(current, context, cancellationToken);

                    if (!stepResult.IsSuccess)
                    {
                        var duration = DateTime.UtcNow - startTime;

                        _logger?.LogError(
                            "Pipeline {PipelineName} failed at step {StepName}. Error: {ErrorMessage}",
                            PipelineName,
                            stepResult.StepName,
                            stepResult.Error?.Message);

                        context.TraceActivity?.SetStatus(ActivityStatusCode.Error, stepResult.Error?.Message);

                        return PipelineResult<TOutput>.Failure(
                            stepResult.Error!,
                            new PipelineMetrics
                            {
                                TotalDuration = duration,
                                StepsExecuted = stepCount,
                                RetryCount = retryCount
                            },
                            context.CorrelationId);
                    }

                    current = stepResult.Output!;
                }
            }

            var totalDuration = DateTime.UtcNow - startTime;

            _logger?.LogInformation(
                "Pipeline {PipelineName} completed successfully in {Duration}ms. Steps: {StepCount}",
                PipelineName,
                totalDuration.TotalMilliseconds,
                stepCount);

            context.TraceActivity?.SetTag("pipeline.duration_ms", totalDuration.TotalMilliseconds);
            context.TraceActivity?.SetStatus(ActivityStatusCode.Ok);

            return PipelineResult<TOutput>.Success(
                (TOutput)current,
                new PipelineMetrics
                {
                    TotalDuration = totalDuration,
                    StepsExecuted = stepCount,
                    RetryCount = retryCount
                },
                context.CorrelationId);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;

            _logger?.LogError(
                ex,
                "Pipeline {PipelineName} failed with unhandled exception",
                PipelineName);

            context.TraceActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            context.TraceActivity?.AddException(ex);

            return PipelineResult<TOutput>.Failure(
                new PipelineError
                {
                    ErrorCode = "PIPELINE_EXECUTION_FAILED",
                    Message = ex.Message,
                    Exception = ex,
                    IsRetryable = false
                },
                new PipelineMetrics
                {
                    TotalDuration = duration,
                    StepsExecuted = stepCount,
                    RetryCount = retryCount
                },
                context.CorrelationId);
        }
    }

    public async IAsyncEnumerable<PipelineResult<TOutput>> ExecuteStreamAsync(
        IAsyncEnumerable<TInput> inputs,
        IPipelineContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation(
            "Pipeline {PipelineName} starting streaming execution. CorrelationId: {CorrelationId}",
            PipelineName,
            context.CorrelationId);

        await foreach (var input in inputs.WithCancellation(cancellationToken))
        {
            var result = await ExecuteAsync(input, context, cancellationToken);
            yield return result;
        }

        _logger?.LogInformation(
            "Pipeline {PipelineName} completed streaming execution",
            PipelineName);
    }

    public async Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        foreach (var step in _steps)
        {
            var stepValidation = await step.ValidateAsync(cancellationToken);
            if (!stepValidation.IsValid)
            {
                errors.AddRange(stepValidation.Errors);
            }
        }

        return errors.Count == 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(errors.ToArray());
    }
}

/// <summary>
/// Exception thrown when a pipeline step fails (used for Polly integration).
/// </summary>
internal sealed class PipelineStepException : Exception
{
    public PipelineError PipelineError { get; }

    public PipelineStepException(PipelineError error)
        : base(error.Message, error.Exception)
    {
        PipelineError = error;
    }
}

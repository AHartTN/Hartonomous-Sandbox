using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Pipelines.Inference;

/// <summary>
/// Factory for creating and executing ensemble inference pipelines with saga pattern.
/// </summary>
public sealed class EnsembleInferencePipelineFactory
{
    private readonly IInferenceOrchestrator _orchestrator;
    private readonly IEmbeddingService _embeddingService;
    private readonly IModelRepository _modelRepository;
    private readonly IInferenceService _inferenceService;
    private readonly IInferenceRequestRepository _inferenceRequestRepository;
    private readonly ILogger<EnsembleInferencePipelineFactory> _logger;
    private readonly ActivitySource? _activitySource;

    public EnsembleInferencePipelineFactory(
        IInferenceOrchestrator orchestrator,
        IEmbeddingService embeddingService,
        IModelRepository modelRepository,
        IInferenceService inferenceService,
        IInferenceRequestRepository inferenceRequestRepository,
        ILogger<EnsembleInferencePipelineFactory> logger,
        ActivitySource? activitySource = null)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _inferenceService = inferenceService ?? throw new ArgumentNullException(nameof(inferenceService));
        _inferenceRequestRepository = inferenceRequestRepository ?? throw new ArgumentNullException(nameof(inferenceRequestRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySource = activitySource;
    }

    /// <summary>
    /// Creates ensemble inference pipeline with standard resilience.
    /// </summary>
    public IPipeline<EnsembleInferenceRequest, EnsembleInferenceResult> CreatePipeline(
        bool enableResilience = true,
        int maxRetries = 3,
        TimeSpan? timeout = null)
    {
        var builder = PipelineBuilder<EnsembleInferenceRequest, EnsembleInferenceResult>
            .Create("ensemble-inference", _logger, _activitySource)
            .AddStep(new SearchCandidateAtomsStep(_orchestrator, _embeddingService))
            .AddStep(new InvokeEnsembleModelsStep(
                _modelRepository,
                _inferenceService,
                _logger.CreateLogger<InvokeEnsembleModelsStep>()))
            .AddStep(new AggregateEnsembleResultsStep())
            .AddStep(new PersistInferenceResultStep(
                _inferenceRequestRepository,
                _logger.CreateLogger<PersistInferenceResultStep>()));

        if (enableResilience)
        {
            builder = builder.WithStandardResilience(
                maxRetries: maxRetries,
                timeout: timeout ?? TimeSpan.FromSeconds(60), // Longer timeout for model invocations
                circuitBreakerFailureThreshold: 0.5);
        }

        return builder.Build();
    }

    /// <summary>
    /// Executes ensemble inference with saga pattern for distributed transaction integrity.
    /// </summary>
    public async Task<EnsembleInferenceResult> InferWithSagaAsync(
        EnsembleInferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var saga = new EnsembleInferenceSaga(_inferenceRequestRepository, _logger.CreateLogger<EnsembleInferenceSaga>());
        var pipeline = CreatePipeline(enableResilience: true);

        var context = PipelineContext.Create(
            _activitySource,
            "ensemble-inference-operation",
            correlationId: Guid.NewGuid().ToString());

        try
        {
            var result = await pipeline.ExecuteAsync(request, context, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogError(
                    "Ensemble inference failed. CorrelationId: {CorrelationId}, Error: {ErrorMessage}",
                    result.CorrelationId,
                    result.Error?.Message);

                // Compensate saga on pipeline failure
                await saga.CompensateAsync(cancellationToken);

                throw new InvalidOperationException(
                    $"Ensemble inference failed: {result.Error?.Message}",
                    result.Error?.Exception);
            }

            // Track created inference request ID for potential compensation
            saga.TrackInferenceRequestCreation(result.Output!.InferenceRequestId);

            return result.Output!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ensemble inference saga encountered error. CorrelationId: {CorrelationId}", context.CorrelationId);

            // Compensate saga on exception
            await saga.CompensateAsync(cancellationToken);

            throw;
        }
    }

    /// <summary>
    /// Convenience method without saga pattern (for non-critical operations).
    /// </summary>
    public async Task<EnsembleInferenceResult> InferAsync(
        EnsembleInferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var pipeline = CreatePipeline(enableResilience: true);

        var context = PipelineContext.Create(
            _activitySource,
            "ensemble-inference-operation",
            correlationId: Guid.NewGuid().ToString());

        var result = await pipeline.ExecuteAsync(request, context, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError(
                "Ensemble inference failed. CorrelationId: {CorrelationId}, Error: {ErrorMessage}",
                result.CorrelationId,
                result.Error?.Message);

            throw new InvalidOperationException(
                $"Ensemble inference failed: {result.Error?.Message}",
                result.Error?.Exception);
        }

        return result.Output!;
    }
}

/// <summary>
/// Extension methods for configuring ensemble inference pipelines.
/// </summary>
public static class EnsembleInferencePipelineExtensions
{
    /// <summary>
    /// Adds ensemble inference pipeline services to DI container.
    /// </summary>
    public static IServiceCollection AddEnsembleInferencePipeline(
        this IServiceCollection services)
    {
        services.AddScoped<EnsembleInferencePipelineFactory>();
        services.AddScoped<EnsembleInferenceSaga>();

        return services;
    }
}

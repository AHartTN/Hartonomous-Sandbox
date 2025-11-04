using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<EnsembleInferencePipelineFactory> _logger;
    private readonly ActivitySource? _activitySource;

    public EnsembleInferencePipelineFactory(
        IInferenceOrchestrator orchestrator,
        IEmbeddingService embeddingService,
        IModelRepository modelRepository,
        IInferenceService inferenceService,
        IInferenceRequestRepository inferenceRequestRepository,
        ILoggerFactory loggerFactory,
        ActivitySource? activitySource = null)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _inferenceService = inferenceService ?? throw new ArgumentNullException(nameof(inferenceService));
        _inferenceRequestRepository = inferenceRequestRepository ?? throw new ArgumentNullException(nameof(inferenceRequestRepository));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<EnsembleInferencePipelineFactory>();
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
        var builder = PipelineBuilder<EnsembleInferenceRequest, EnsembleInferenceRequest>
            .Create("ensemble-inference", _logger, _activitySource)
            .AddStep(new SearchCandidateAtomsStep(_orchestrator, _embeddingService)) // Request -> CandidateRetrieval
            .AddStep(new InvokeEnsembleModelsStep(
                _modelRepository,
                _inferenceService,
                _loggerFactory.CreateLogger<InvokeEnsembleModelsStep>())) // CandidateRetrieval -> EnsembleInvocation
            .AddStep(new AggregateEnsembleResultsStep()) // EnsembleInvocation -> Aggregation
            .AddStep(new PersistInferenceResultStep(
                _inferenceRequestRepository,
                _loggerFactory.CreateLogger<PersistInferenceResultStep>())); // Aggregation -> Result

        if (enableResilience)
        {
            var resilientBuilder = builder.WithStandardResilience(
                maxRetries: maxRetries,
                timeout: timeout ?? TimeSpan.FromSeconds(60), // Longer timeout for model invocations
                circuitBreakerFailureThreshold: 0.5);
            
            return resilientBuilder.Build();
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
        var saga = new EnsembleInferenceSaga(_inferenceRequestRepository, _loggerFactory.CreateLogger<EnsembleInferenceSaga>());
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
            if (result.Output!.InferenceRequestId.HasValue)
            {
                saga.TrackInferenceRequestCreation(result.Output.InferenceRequestId.Value);
            }

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

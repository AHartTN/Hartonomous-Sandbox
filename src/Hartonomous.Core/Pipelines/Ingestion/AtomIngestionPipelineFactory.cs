using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;

namespace Hartonomous.Core.Pipelines.Ingestion;

/// <summary>
/// Factory for creating and executing the atom ingestion pipeline.
/// Demonstrates fluent pipeline composition with SOLID principles.
/// </summary>
public sealed class AtomIngestionPipelineFactory
{
    private readonly IAtomRepository _atomRepository;
    private readonly IAtomEmbeddingRepository _embeddingRepository;
    private readonly IDeduplicationPolicyRepository _policyRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly ActivitySource? _activitySource;

    public AtomIngestionPipelineFactory(
        IAtomRepository atomRepository,
        IAtomEmbeddingRepository embeddingRepository,
        IDeduplicationPolicyRepository policyRepository,
        IEmbeddingService embeddingService,
        ILoggerFactory loggerFactory,
        ActivitySource? activitySource = null)
    {
        _atomRepository = atomRepository ?? throw new ArgumentNullException(nameof(atomRepository));
        _embeddingRepository = embeddingRepository ?? throw new ArgumentNullException(nameof(embeddingRepository));
        _policyRepository = policyRepository ?? throw new ArgumentNullException(nameof(policyRepository));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<AtomIngestionPipelineFactory>();
        _activitySource = activitySource;
    }

    /// <summary>
    /// Creates a new atom ingestion pipeline with standard resilience policies.
    /// </summary>
    /// <param name="enableResilience">If true, adds retry/circuit breaker/timeout policies.</param>
    /// <param name="maxRetries">Maximum retry attempts for transient failures.</param>
    /// <param name="timeout">Maximum execution time per step.</param>
    /// <returns>A configured pipeline ready for execution.</returns>
    public IPipeline<AtomIngestionPipelineRequest, AtomIngestionPipelineResult> CreatePipeline(
        bool enableResilience = true,
        int maxRetries = 3,
        TimeSpan? timeout = null)
    {
        var builder = PipelineBuilder<AtomIngestionPipelineRequest, AtomIngestionPipelineRequest>
            .Create("atom-ingestion", _logger, _activitySource)
            .AddStep(new ComputeContentHashStep())
            .AddStep(new CheckExactDuplicateStep(_atomRepository))
            .AddStep(new GenerateEmbeddingStep(
                _embeddingService,
                _loggerFactory.CreateLogger<GenerateEmbeddingStep>()))
            .AddStep(new CheckSemanticDuplicateStep(
                _embeddingRepository,
                _policyRepository,
                _atomRepository))
            .AddStep(new PersistAtomStep(
                _atomRepository,
                _embeddingRepository,
                _loggerFactory.CreateLogger<PersistAtomStep>()));

        if (enableResilience)
        {
            builder = builder.WithStandardResilience(
                maxRetries: maxRetries,
                timeout: timeout ?? TimeSpan.FromSeconds(30),
                circuitBreakerFailureThreshold: 0.5);
        }

        return builder.Build();
    }

    /// <summary>
    /// Convenience method to create and execute the pipeline in one call.
    /// </summary>
    public async Task<AtomIngestionPipelineResult> IngestAtomAsync(
        AtomIngestionPipelineRequest request,
        CancellationToken cancellationToken = default)
    {
        var pipeline = CreatePipeline(enableResilience: true);

        var context = PipelineContext.Create(
            _activitySource,
            "atom-ingestion-operation",
            correlationId: Guid.NewGuid().ToString());

        var result = await pipeline.ExecuteAsync(request, context, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError(
                "Atom ingestion failed. CorrelationId: {CorrelationId}, Error: {ErrorMessage}",
                result.CorrelationId,
                result.Error?.Message);

            throw new InvalidOperationException(
                $"Atom ingestion failed: {result.Error?.Message}",
                result.Error?.Exception);
        }

        return result.Output!;
    }

    /// <summary>
    /// Creates a custom pipeline with specific resilience configuration.
    /// Demonstrates extensibility - callers can customize resilience per use case.
    /// </summary>
    public IPipeline<AtomIngestionPipelineRequest, AtomIngestionPipelineResult> CreateCustomPipeline(
        Action<Polly.ResiliencePipelineBuilder> configureResilience)
    {
        return PipelineBuilder<AtomIngestionPipelineRequest, AtomIngestionPipelineRequest>
            .Create("atom-ingestion-custom", _logger, _activitySource)
            .AddStep(new ComputeContentHashStep())
            .AddStep(new CheckExactDuplicateStep(_atomRepository))
            .AddStep(new GenerateEmbeddingStep(
                _embeddingService,
                _loggerFactory.CreateLogger<GenerateEmbeddingStep>()))
            .AddStep(new CheckSemanticDuplicateStep(
                _embeddingRepository,
                _policyRepository,
                _atomRepository))
            .AddStep(new PersistAtomStep(
                _atomRepository,
                _embeddingRepository,
                _loggerFactory.CreateLogger<PersistAtomStep>()))
            .WithResilience(configureResilience)
            .Build();
    }
}

/// <summary>
/// Example: Adapter to maintain backward compatibility with IAtomIngestionService.
/// Demonstrates how the pipeline pattern integrates with existing interfaces.
/// </summary>
public sealed class AtomIngestionServiceAdapter : IAtomIngestionService
{
    private readonly AtomIngestionPipelineFactory _pipelineFactory;
    private readonly ILogger<AtomIngestionServiceAdapter> _logger;

    public AtomIngestionServiceAdapter(
        AtomIngestionPipelineFactory pipelineFactory,
        ILogger<AtomIngestionServiceAdapter> logger)
    {
        _pipelineFactory = pipelineFactory ?? throw new ArgumentNullException(nameof(pipelineFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AtomIngestionResult> IngestAsync(
        AtomIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Map old DTOs to new pipeline request
        var pipelineRequest = new AtomIngestionPipelineRequest
        {
            HashInput = request.HashInput,
            Modality = request.Modality,
            Subtype = request.Subtype,
            SourceUri = request.SourceUri,
            SourceType = request.SourceType,
            CanonicalText = request.CanonicalText,
            Metadata = request.Metadata,
            PayloadLocator = request.PayloadLocator,
            EmbeddingType = request.EmbeddingType ?? "default",
            ModelId = request.ModelId,
            PolicyName = request.PolicyName ?? "default"
        };

        var pipelineResult = await _pipelineFactory.IngestAtomAsync(pipelineRequest, cancellationToken);

        // Map pipeline result to interface contract
        return new AtomIngestionResult
        {
            Atom = pipelineResult.Atom,
            Embedding = pipelineResult.Embedding,
            WasDuplicate = pipelineResult.WasDuplicate,
            DuplicateReason = pipelineResult.DuplicateReason,
            SemanticSimilarity = pipelineResult.SemanticSimilarity
        };
    }
}

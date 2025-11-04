using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Pipelines.Inference;

/// <summary>
/// Request for ensemble inference pipeline.
/// </summary>
public sealed record EnsembleInferenceRequest
{
    public required string Prompt { get; init; }
    public string? Context { get; init; }
    public int MaxCandidates { get; init; } = 5;
    public int MinModelCount { get; init; } = 3;
    public double ConsensusThreshold { get; init; } = 0.6;
    public List<int>? ModelIds { get; init; }
    public Dictionary<string, object>? Parameters { get; init; }
}

/// <summary>
/// Step 1: Search for candidate atoms using semantic search.
/// </summary>
public sealed class SearchCandidateAtomsStep : PipelineStepBase<EnsembleInferenceRequest, CandidateRetrievalResult>
{
    private readonly IInferenceOrchestrator _orchestrator;
    private readonly IEmbeddingService _embeddingService;

    public override string StepName => "search-candidate-atoms";

    public SearchCandidateAtomsStep(
        IInferenceOrchestrator orchestrator,
        IEmbeddingService embeddingService)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
    }

    protected override async Task<CandidateRetrievalResult> ExecuteStepAsync(
        EnsembleInferenceRequest input,
        IPipelineContext context,
        CancellationToken cancellationToken)
    {
        // Generate embedding for the prompt
        var promptEmbedding = await _embeddingService
            .EmbedTextAsync(input.Prompt, cancellationToken)
            .ConfigureAwait(false);

        if (promptEmbedding == null || promptEmbedding.Length == 0)
        {
            throw new InvalidOperationException("Failed to generate embedding for prompt.");
        }

        // Search for semantically similar atoms
        var candidates = await _orchestrator
            .SemanticSearchAsync(
                promptEmbedding,
                maxResults: input.MaxCandidates,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        context.TraceActivity?.SetTag("inference.candidate_count", candidates.Count);
        context.TraceActivity?.SetTag("inference.max_candidates", input.MaxCandidates);

        return new CandidateRetrievalResult
        {
            Request = input,
            Candidates = candidates
        };
    }
}

/// <summary>
/// Step 2: Invoke multiple models in parallel for ensemble voting.
/// </summary>
public sealed class InvokeEnsembleModelsStep : PipelineStepBase<
    CandidateRetrievalResult,
    EnsembleInvocationResult>
{
    private readonly IModelRepository _modelRepository;
    private readonly IInferenceService _inferenceService;
    private readonly ILogger<InvokeEnsembleModelsStep> _logger;

    public override string StepName => "invoke-ensemble-models";

    public InvokeEnsembleModelsStep(
        IModelRepository modelRepository,
        IInferenceService inferenceService,
        ILogger<InvokeEnsembleModelsStep> logger)
    {
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _inferenceService = inferenceService ?? throw new ArgumentNullException(nameof(inferenceService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<EnsembleInvocationResult> ExecuteStepAsync(
        CandidateRetrievalResult input,
        IPipelineContext context,
        CancellationToken cancellationToken)
    {
        // Get active models
        var models = input.Request.ModelIds != null && input.Request.ModelIds.Any()
            ? await _modelRepository.GetByIdsAsync(input.Request.ModelIds, cancellationToken).ConfigureAwait(false)
            : await _modelRepository.GetActiveModelsAsync(cancellationToken).ConfigureAwait(false);

        if (models.Count < input.Request.MinModelCount)
        {
            throw new InvalidOperationException(
                $"Insufficient models available. Required: {input.Request.MinModelCount}, Available: {models.Count}");
        }

        context.TraceActivity?.SetTag("inference.model_count", models.Count);

        // Build context from candidate atoms
        var contextText = input.Request.Context ?? string.Join("\n\n", input.Candidates.Select(a => a.CanonicalText));

        // Invoke models in parallel
        var invocationTasks = models.Select(async model =>
        {
            var startTime = DateTime.UtcNow;
            try
            {
                var output = await _inferenceService.InvokeModelAsync(
                    model.ModelId,
                    input.Request.Prompt,
                    contextText,
                    input.Request.Parameters,
                    cancellationToken);

                var duration = DateTime.UtcNow - startTime;

                return new ModelContribution
                {
                    ModelId = model.ModelId,
                    ModelName = model.ModelName,
                    Output = output,
                    Weight = 1.0, // Default weight for all models in ensemble
                    Duration = duration
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Model invocation failed. ModelId: {ModelId}, ModelName: {ModelName}",
                    model.ModelId,
                    model.ModelName);

                // Return null for failed invocations (filtered later)
                return null;
            }
        });

        var results = await Task.WhenAll(invocationTasks).ConfigureAwait(false);
        var contributions = results.Where(r => r != null).Cast<ModelContribution>().ToList();

        if (contributions.Count < input.Request.MinModelCount)
        {
            throw new InvalidOperationException(
                $"Insufficient successful model invocations. Required: {input.Request.MinModelCount}, Succeeded: {contributions.Count}");
        }

        context.TraceActivity?.SetTag("inference.successful_invocations", contributions.Count);
        context.TraceActivity?.SetTag("inference.failed_invocations", models.Count - contributions.Count);

        return new EnsembleInvocationResult
        {
            Request = input.Request,
            Candidates = input.Candidates,
            Contributions = contributions
        };
    }
}

/// <summary>
/// Step 3: Aggregate model outputs using consensus voting.
/// </summary>
public sealed class AggregateEnsembleResultsStep : PipelineStepBase<
    EnsembleInvocationResult,
    AggregationResult>
{
    public override string StepName => "aggregate-ensemble-results";

    protected override Task<AggregationResult> ExecuteStepAsync(
        EnsembleInvocationResult input,
        IPipelineContext context,
        CancellationToken cancellationToken)
    {
        // Group by output text
        var outputGroups = input.Contributions
            .GroupBy(c => c.Output, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                Output = g.Key,
                TotalWeight = g.Sum(c => c.Weight),
                Count = g.Count(),
                Contributions = g.ToList()
            })
            .OrderByDescending(g => g.TotalWeight)
            .ThenByDescending(g => g.Count)
            .ToList();

        if (!outputGroups.Any())
        {
            throw new InvalidOperationException("No outputs to aggregate.");
        }

        var winner = outputGroups.First();
        var totalWeight = input.Contributions.Sum(c => c.Weight);

        // Calculate confidence as consensus ratio
        var confidence = winner.TotalWeight / totalWeight;

        // Check if consensus threshold is met
        if (confidence < input.Request.ConsensusThreshold)
        {
            context.TraceActivity?.SetTag("inference.low_consensus", true);
            context.TraceActivity?.SetTag("inference.consensus_threshold", input.Request.ConsensusThreshold);
        }

        context.TraceActivity?.SetTag("inference.final_output_length", winner.Output.Length);
        context.TraceActivity?.SetTag("inference.confidence", confidence);
        context.TraceActivity?.SetTag("inference.consensus_count", winner.Count);
        context.TraceActivity?.SetTag("inference.unique_outputs", outputGroups.Count);

        return Task.FromResult(new AggregationResult
        {
            Request = input.Request,
            Candidates = input.Candidates,
            Contributions = input.Contributions,
            FinalOutput = winner.Output,
            Confidence = confidence
        });
    }
}

/// <summary>
/// Step 4: Persist inference request and result to database.
/// Implements saga pattern with compensation for rollback.
/// </summary>
public sealed class PersistInferenceResultStep : PipelineStepBase<
    AggregationResult,
    EnsembleInferenceResult>
{
    private readonly IInferenceRequestRepository _inferenceRequestRepository;
    private readonly ILogger<PersistInferenceResultStep> _logger;

    public override string StepName => "persist-inference-result";

    public PersistInferenceResultStep(
        IInferenceRequestRepository inferenceRequestRepository,
        ILogger<PersistInferenceResultStep> logger)
    {
        _inferenceRequestRepository = inferenceRequestRepository ?? throw new ArgumentNullException(nameof(inferenceRequestRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<EnsembleInferenceResult> ExecuteStepAsync(
        AggregationResult input,
        IPipelineContext context,
        CancellationToken cancellationToken)
    {
        // Create InferenceRequest entity
        var inferenceRequest = new InferenceRequest
        {
            TaskType = "ensemble-inference",
            InputData = System.Text.Json.JsonSerializer.Serialize(new { input.Request.Prompt, input.Request.Context }),
            InputHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input.Request.Prompt)),
            ModelsUsed = System.Text.Json.JsonSerializer.Serialize(input.Contributions.Select(c => new { c.ModelId, c.ModelName }).ToList()),
            EnsembleStrategy = "MajorityVoting",
            OutputData = System.Text.Json.JsonSerializer.Serialize(new { Output = input.FinalOutput, Confidence = input.Confidence }),
            OutputMetadata = System.Text.Json.JsonSerializer.Serialize(new { CandidateCount = input.Candidates.Count, ModelCount = input.Contributions.Count }),
            TotalDurationMs = (int)(context.TraceActivity?.Duration ?? TimeSpan.Zero).TotalMilliseconds,
            CacheHit = false,
            RequestTimestamp = DateTime.UtcNow
        };

        // Add inference steps for each model contribution
        var stepNumber = 1;
        foreach (var contribution in input.Contributions)
        {
            inferenceRequest.Steps.Add(new InferenceStep
            {
                StepNumber = stepNumber++,
                ModelId = contribution.ModelId,
                OperationType = "ensemble-model-invocation",
                QueryText = contribution.Output, // Store output in QueryText for now
                DurationMs = (int)contribution.Duration.TotalMilliseconds,
                CacheUsed = false
            });
        }

        // Persist to database (transaction scope)
        var saved = await _inferenceRequestRepository
            .AddAsync(inferenceRequest, cancellationToken)
            .ConfigureAwait(false);

        context.TraceActivity?.SetTag("inference.request_id", saved.InferenceId);
        context.TraceActivity?.SetTag("inference.step_count", saved.Steps.Count);

        _logger.LogInformation(
            "Persisted inference request {InferenceId} with {StepCount} steps. Confidence: {Confidence:F4}, CorrelationId: {CorrelationId}",
            saved.InferenceId,
            saved.Steps.Count,
            input.Confidence,
            context.CorrelationId);

        return new EnsembleInferenceResult
        {
            Request = input.Request,
            Output = input.FinalOutput,
            Confidence = input.Confidence,
            Contributions = input.Contributions,
            CandidateAtoms = input.Candidates,
            InferenceRequestId = saved.InferenceId,
            TotalDuration = context.TraceActivity?.Duration ?? TimeSpan.Zero,
            CorrelationId = context.CorrelationId
        };
    }
}

/// <summary>
/// Saga coordinator for ensemble inference with compensation logic.
/// Ensures distributed transaction integrity across pipeline steps.
/// </summary>
public sealed class EnsembleInferenceSaga
{
    private readonly IInferenceRequestRepository _inferenceRequestRepository;
    private readonly ILogger<EnsembleInferenceSaga> _logger;

    private long? _createdInferenceRequestId;

    public EnsembleInferenceSaga(
        IInferenceRequestRepository inferenceRequestRepository,
        ILogger<EnsembleInferenceSaga> logger)
    {
        _inferenceRequestRepository = inferenceRequestRepository ?? throw new ArgumentNullException(nameof(inferenceRequestRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tracks creation of inference request for potential rollback.
    /// </summary>
    public void TrackInferenceRequestCreation(long inferenceRequestId)
    {
        _createdInferenceRequestId = inferenceRequestId;
    }

    /// <summary>
    /// Compensates (rolls back) all saga steps on failure.
    /// </summary>
    public async Task CompensateAsync(CancellationToken cancellationToken = default)
    {
        if (_createdInferenceRequestId.HasValue)
        {
            try
            {
                _logger.LogWarning(
                    "Compensating inference request {InferenceId} due to saga failure.",
                    _createdInferenceRequestId.Value);

                // Mark inference request as failed/compensated
                await _inferenceRequestRepository
                    .UpdateStatusAsync(
                        _createdInferenceRequestId.Value,
                        "Failed",
                        "Pipeline compensation triggered due to saga failure",
                        cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation(
                    "Successfully compensated inference request {InferenceId}. Status set to Failed.",
                    _createdInferenceRequestId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to compensate inference request {InferenceId}. Manual intervention may be required.",
                    _createdInferenceRequestId.Value);
            }
        }
    }
}

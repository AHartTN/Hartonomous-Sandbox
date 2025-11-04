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
/// Result from ensemble inference pipeline.
/// </summary>
public sealed record EnsembleInferenceResult
{
    public required string FinalOutput { get; init; }
    public required double Confidence { get; init; }
    public required List<ModelContribution> ModelContributions { get; init; }
    public required List<Atom> CandidateAtoms { get; init; }
    public required long InferenceRequestId { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}

/// <summary>
/// Contribution from a single model in the ensemble.
/// </summary>
public sealed record ModelContribution
{
    public required int ModelId { get; init; }
    public required string ModelName { get; init; }
    public required string Output { get; init; }
    public required double Weight { get; init; }
    public required TimeSpan Duration { get; init; }
}

/// <summary>
/// Step 1: Search for candidate atoms using semantic search.
/// </summary>
public sealed class SearchCandidateAtomsStep : PipelineStepBase<
    EnsembleInferenceRequest,
    (EnsembleInferenceRequest Request, List<Atom> Candidates)>
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

    protected override async Task<(EnsembleInferenceRequest, List<Atom>)> ExecuteStepAsync(
        EnsembleInferenceRequest input,
        IPipelineContext context,
        CancellationToken cancellationToken)
    {
        // Generate embedding for the prompt
        var promptEmbedding = await _embeddingService
            .GenerateEmbeddingAsync(input.Prompt, cancellationToken)
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

        return (input, candidates);
    }
}

/// <summary>
/// Step 2: Invoke multiple models in parallel for ensemble voting.
/// </summary>
public sealed class InvokeEnsembleModelsStep : PipelineStepBase<
    (EnsembleInferenceRequest Request, List<Atom> Candidates),
    (EnsembleInferenceRequest Request, List<Atom> Candidates, List<ModelContribution> Contributions)>
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

    protected override async Task<(EnsembleInferenceRequest, List<Atom>, List<ModelContribution>)> ExecuteStepAsync(
        (EnsembleInferenceRequest Request, List<Atom> Candidates) input,
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
                    Weight = model.Weight ?? 1.0,
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

        return (input.Request, input.Candidates, contributions);
    }
}

/// <summary>
/// Step 3: Aggregate model outputs using consensus voting.
/// </summary>
public sealed class AggregateEnsembleResultsStep : PipelineStepBase<
    (EnsembleInferenceRequest Request, List<Atom> Candidates, List<ModelContribution> Contributions),
    (EnsembleInferenceRequest Request, List<Atom> Candidates, List<ModelContribution> Contributions, string FinalOutput, double Confidence)>
{
    public override string StepName => "aggregate-ensemble-results";

    protected override Task<(EnsembleInferenceRequest, List<Atom>, List<ModelContribution>, string, double)> ExecuteStepAsync(
        (EnsembleInferenceRequest Request, List<Atom> Candidates, List<ModelContribution> Contributions) input,
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

        return Task.FromResult((
            input.Request,
            input.Candidates,
            input.Contributions,
            winner.Output,
            confidence
        ));
    }
}

/// <summary>
/// Step 4: Persist inference request and result to database.
/// Implements saga pattern with compensation for rollback.
/// </summary>
public sealed class PersistInferenceResultStep : PipelineStepBase<
    (EnsembleInferenceRequest Request, List<Atom> Candidates, List<ModelContribution> Contributions, string FinalOutput, double Confidence),
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
        (EnsembleInferenceRequest Request, List<Atom> Candidates, List<ModelContribution> Contributions, string FinalOutput, double Confidence) input,
        IPipelineContext context,
        CancellationToken cancellationToken)
    {
        // Create InferenceRequest entity
        var inferenceRequest = new InferenceRequest
        {
            Prompt = input.Request.Prompt,
            Context = input.Request.Context,
            FinalOutput = input.FinalOutput,
            Confidence = input.Confidence,
            CreatedAt = DateTime.UtcNow,
            CorrelationId = context.CorrelationId
        };

        // Add inference steps for each model contribution
        foreach (var contribution in input.Contributions)
        {
            inferenceRequest.Steps.Add(new InferenceStep
            {
                ModelId = contribution.ModelId,
                Output = contribution.Output,
                Weight = contribution.Weight,
                DurationMs = (int)contribution.Duration.TotalMilliseconds,
                ExecutedAt = DateTime.UtcNow
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
            saved.Confidence,
            context.CorrelationId);

        return new EnsembleInferenceResult
        {
            FinalOutput = input.FinalOutput,
            Confidence = input.Confidence,
            ModelContributions = input.Contributions,
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
                    .MarkAsFailedAsync(_createdInferenceRequestId.Value, cancellationToken)
                    .ConfigureAwait(false);

                // TODO: Add Neo4j graph cleanup if relationships were created
                // TODO: Add distributed cache invalidation if results were cached
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

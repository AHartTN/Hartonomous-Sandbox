using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Pipelines.Ingestion;

/// <summary>
/// Pipeline request for atom ingestion with embedding generation.
/// </summary>
public sealed record AtomIngestionPipelineRequest
{
    public required string HashInput { get; init; }
    public required string Modality { get; init; }
    public string? Subtype { get; init; }
    public string? SourceUri { get; init; }
    public string? SourceType { get; init; }
    public string? CanonicalText { get; init; }
    public string? Metadata { get; init; }
    public string? PayloadLocator { get; init; }
    public string EmbeddingType { get; init; } = "default";
    public int? ModelId { get; init; }
    public string PolicyName { get; init; } = "default";
}

/// <summary>
/// Pipeline result for atom ingestion.
/// </summary>
public sealed record AtomIngestionPipelineResult
{
    public required Atom Atom { get; init; }
    public AtomEmbedding? Embedding { get; init; }
    public bool WasDuplicate { get; init; }
    public string? DuplicateReason { get; init; }
    public double? SemanticSimilarity { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}

/// <summary>
/// Step 1: Compute content hash for deduplication.
/// </summary>
public sealed class ComputeContentHashStep : PipelineStepBase<AtomIngestionPipelineRequest, HashComputationResult>
{
    public override string StepName => "compute-content-hash";

    protected override Task<HashComputationResult> ExecuteStepAsync(
        AtomIngestionPipelineRequest input,
        IPipelineContext context,
        CancellationToken cancellationToken)
    {
        var contentHash = HashUtility.ComputeSHA256Bytes(input.HashInput);

        context.TraceActivity?.SetTag("atom.hash_length", contentHash.Length);
        context.TraceActivity?.SetTag("atom.modality", input.Modality);
        context.TraceActivity?.SetTag("atom.subtype", input.Subtype ?? "none");

        var result = new HashComputationResult
        {
            Request = input,
            ContentHash = contentHash
        };

        return Task.FromResult(result);
    }
}

/// <summary>
/// Step 2: Check for exact content hash duplicates.
/// </summary>
public sealed class CheckExactDuplicateStep : PipelineStepBase<HashComputationResult, ExactDuplicateCheckResult>
{
    private readonly IAtomRepository _atomRepository;

    public override string StepName => "check-exact-duplicate";

    public CheckExactDuplicateStep(IAtomRepository atomRepository)
    {
        _atomRepository = atomRepository ?? throw new ArgumentNullException(nameof(atomRepository));
    }

    protected override async Task<ExactDuplicateCheckResult> ExecuteStepAsync(
        HashComputationResult input,
        IPipelineContext context,
        CancellationToken cancellationToken)
    {
        var existing = await _atomRepository
            .GetByContentHashAsync(input.ContentHash, cancellationToken)
            .ConfigureAwait(false);

        if (existing != null)
        {
            context.TraceActivity?.SetTag("atom.duplicate.exact", true);
            context.TraceActivity?.SetTag("atom.duplicate.atom_id", existing.AtomId);

            // Increment reference count
            await _atomRepository
                .IncrementReferenceCountAsync(existing.AtomId, 1, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            context.TraceActivity?.SetTag("atom.duplicate.exact", false);
        }

        return new ExactDuplicateCheckResult
        {
            Request = input.Request,
            ContentHash = input.ContentHash,
            ExistingAtom = existing
        };
    }
}

/// <summary>
/// Step 3: Generate embedding if not a duplicate (optimized to skip if duplicate found).
/// </summary>
public sealed class GenerateEmbeddingStep : PipelineStepBase<ExactDuplicateCheckResult, EmbeddingGenerationResult>
{
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<GenerateEmbeddingStep> _logger;

    public override string StepName => "generate-embedding";

    public GenerateEmbeddingStep(
        IEmbeddingService embeddingService,
        ILogger<GenerateEmbeddingStep> logger)
    {
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<EmbeddingGenerationResult> ExecuteStepAsync(
        ExactDuplicateCheckResult input,
        IPipelineContext context,
        CancellationToken cancellationToken)
    {
        // Skip embedding generation if duplicate found
        if (input.ExistingAtom != null)
        {
            context.TraceActivity?.SetTag("embedding.skipped", true);
            context.TraceActivity?.SetTag("embedding.skip_reason", "exact_duplicate");
            
            return new EmbeddingGenerationResult
            {
                Request = input.Request,
                ContentHash = input.ContentHash,
                ExistingAtom = input.ExistingAtom,
                EmbeddingVector = Array.Empty<float>()
            };
        }

        // Only generate embedding if we have text content
        if (string.IsNullOrWhiteSpace(input.Request.CanonicalText))
        {
            context.TraceActivity?.SetTag("embedding.skipped", true);
            context.TraceActivity?.SetTag("embedding.skip_reason", "no_text_content");
            
            return new EmbeddingGenerationResult
            {
                Request = input.Request,
                ContentHash = input.ContentHash,
                ExistingAtom = null,
                EmbeddingVector = Array.Empty<float>()
            };
        }

        try
        {
            _logger.LogDebug(
                "Generating embedding for atom. Modality: {Modality}, TextLength: {TextLength}",
                input.Request.Modality,
                input.Request.CanonicalText.Length);

            // Generate embedding using the embedding service
            var embedding = await _embeddingService.EmbedTextAsync(
                input.Request.CanonicalText,
                cancellationToken);

            context.TraceActivity?.SetTag("embedding.generated", true);
            context.TraceActivity?.SetTag("embedding.dimension", embedding?.Length ?? 0);

            return new EmbeddingGenerationResult
            {
                Request = input.Request,
                ContentHash = input.ContentHash,
                ExistingAtom = null,
                EmbeddingVector = embedding ?? Array.Empty<float>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to generate embedding for atom. Continuing without embedding.");

            context.TraceActivity?.SetTag("embedding.failed", true);
            context.TraceActivity?.SetTag("embedding.error", ex.Message);

            // Continue pipeline without embedding (non-fatal)
            return new EmbeddingGenerationResult
            {
                Request = input.Request,
                ContentHash = input.ContentHash,
                ExistingAtom = null,
                EmbeddingVector = Array.Empty<float>()
            };
        }
    }
}

/// <summary>
/// Step 4: Check for semantic duplicates if embedding was generated.
/// </summary>
public sealed class CheckSemanticDuplicateStep : PipelineStepBase<EmbeddingGenerationResult, SemanticDuplicateCheckResult>
{
    private readonly IAtomEmbeddingRepository _embeddingRepository;
    private readonly IDeduplicationPolicyRepository _policyRepository;
    private readonly IAtomRepository _atomRepository;

    public override string StepName => "check-semantic-duplicate";

    public CheckSemanticDuplicateStep(
        IAtomEmbeddingRepository embeddingRepository,
        IDeduplicationPolicyRepository policyRepository,
        IAtomRepository atomRepository)
    {
        _embeddingRepository = embeddingRepository ?? throw new ArgumentNullException(nameof(embeddingRepository));
        _policyRepository = policyRepository ?? throw new ArgumentNullException(nameof(policyRepository));
        _atomRepository = atomRepository ?? throw new ArgumentNullException(nameof(atomRepository));
    }

    protected override async Task<SemanticDuplicateCheckResult> ExecuteStepAsync(
        EmbeddingGenerationResult input,
        IPipelineContext context,
        CancellationToken cancellationToken)
    {
        // Skip if exact duplicate already found
        if (input.ExistingAtom != null)
        {
            context.TraceActivity?.SetTag("semantic_check.skipped", true);
            context.TraceActivity?.SetTag("semantic_check.skip_reason", "exact_duplicate_found");
            
            return new SemanticDuplicateCheckResult
            {
                Request = input.Request,
                ContentHash = input.ContentHash,
                ExistingAtom = input.ExistingAtom,
                EmbeddingVector = input.EmbeddingVector,
                SimilarAtom = null,
                SimilarityScore = null
            };
        }

        // Skip if no embedding generated
        if (input.EmbeddingVector == null || input.EmbeddingVector.Length == 0)
        {
            context.TraceActivity?.SetTag("semantic_check.skipped", true);
            context.TraceActivity?.SetTag("semantic_check.skip_reason", "no_embedding");
            
            return new SemanticDuplicateCheckResult
            {
                Request = input.Request,
                ContentHash = input.ContentHash,
                ExistingAtom = null,
                EmbeddingVector = Array.Empty<float>(),
                SimilarAtom = null,
                SimilarityScore = null
            };
        }

        // Get deduplication policy
        var policy = await _policyRepository
            .GetActivePolicyAsync(input.Request.PolicyName, cancellationToken)
            .ConfigureAwait(false);

        if (policy?.SemanticThreshold == null)
        {
            context.TraceActivity?.SetTag("semantic_check.skipped", true);
            context.TraceActivity?.SetTag("semantic_check.skip_reason", "no_policy");
            
            return new SemanticDuplicateCheckResult
            {
                Request = input.Request,
                ContentHash = input.ContentHash,
                ExistingAtom = null,
                EmbeddingVector = input.EmbeddingVector,
                SimilarAtom = null,
                SimilarityScore = null
            };
        }

        // Pad embedding to SQL vector length
        var padded = VectorUtility.PadToSqlLength(input.EmbeddingVector, out var usedPadding);
        var sqlVector = padded.ToSqlVector();

        var semanticThreshold = Math.Clamp(policy.SemanticThreshold.Value, -1d, 1d);
        var maxCosineDistance = Math.Max(0d, 1d - semanticThreshold);

        if (maxCosineDistance >= 2d)
        {
            context.TraceActivity?.SetTag("semantic_check.skipped", true);
            context.TraceActivity?.SetTag("semantic_check.skip_reason", "threshold_too_high");
            
            return new SemanticDuplicateCheckResult
            {
                Request = input.Request,
                ContentHash = input.ContentHash,
                ExistingAtom = null,
                EmbeddingVector = input.EmbeddingVector,
                SimilarAtom = null,
                SimilarityScore = null
            };
        }

        // Search for semantic duplicates
        var semanticDuplicate = await _embeddingRepository
            .FindNearestBySimilarityAsync(
                sqlVector,
                input.Request.EmbeddingType,
                input.Request.ModelId,
                maxCosineDistance,
                cancellationToken)
            .ConfigureAwait(false);

        if (semanticDuplicate == null)
        {
            context.TraceActivity?.SetTag("atom.duplicate.semantic", false);
            
            return new SemanticDuplicateCheckResult
            {
                Request = input.Request,
                ContentHash = input.ContentHash,
                ExistingAtom = null,
                EmbeddingVector = input.EmbeddingVector,
                SimilarAtom = null,
                SimilarityScore = null
            };
        }

        // Load the duplicate atom
        var duplicateAtom = semanticDuplicate.Embedding.Atom
            ?? await _atomRepository.GetByIdAsync(semanticDuplicate.Embedding.AtomId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Failed to load atom {semanticDuplicate.Embedding.AtomId} for semantic duplicate.");

        // Increment reference count
        await _atomRepository
            .IncrementReferenceCountAsync(duplicateAtom.AtomId, 1, cancellationToken)
            .ConfigureAwait(false);

        var similarity = 1d - semanticDuplicate.CosineDistance;

        context.TraceActivity?.SetTag("atom.duplicate.semantic", true);
        context.TraceActivity?.SetTag("atom.duplicate.similarity", similarity);
        context.TraceActivity?.SetTag("atom.duplicate.threshold", semanticThreshold);
        context.TraceActivity?.SetTag("atom.duplicate.atom_id", duplicateAtom.AtomId);

        return new SemanticDuplicateCheckResult
        {
            Request = input.Request,
            ContentHash = input.ContentHash,
            ExistingAtom = null,
            EmbeddingVector = input.EmbeddingVector,
            SimilarAtom = duplicateAtom,
            SimilarityScore = (float)similarity
        };
    }
}

/// <summary>
/// Step 5: Persist new atom if no duplicates found.
/// </summary>
public sealed class PersistAtomStep : PipelineStepBase<SemanticDuplicateCheckResult, AtomIngestionPipelineResult>
{
    private readonly IAtomRepository _atomRepository;
    private readonly IAtomEmbeddingRepository _embeddingRepository;
    private readonly ILogger<PersistAtomStep> _logger;

    public override string StepName => "persist-atom";

    public PersistAtomStep(
        IAtomRepository atomRepository,
        IAtomEmbeddingRepository embeddingRepository,
        ILogger<PersistAtomStep> logger)
    {
        _atomRepository = atomRepository ?? throw new ArgumentNullException(nameof(atomRepository));
        _embeddingRepository = embeddingRepository ?? throw new ArgumentNullException(nameof(embeddingRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<AtomIngestionPipelineResult> ExecuteStepAsync(
        SemanticDuplicateCheckResult input,
        IPipelineContext context,
        CancellationToken cancellationToken)
    {
        // Return exact duplicate
        if (input.ExistingAtom != null)
        {
            context.TraceActivity?.SetTag("atom.persisted", false);
            context.TraceActivity?.SetTag("atom.duplicate_type", "exact");

            // Load embedding for existing atom
            var embedding = input.ExistingAtom.Embeddings?.FirstOrDefault();

            return new AtomIngestionPipelineResult
            {
                Atom = input.ExistingAtom,
                Embedding = embedding,
                WasDuplicate = true,
                DuplicateReason = "Exact content hash match",
                SemanticSimilarity = null,
                TotalDuration = context.TraceActivity?.Duration ?? TimeSpan.Zero,
                CorrelationId = context.CorrelationId
            };
        }

        // Return semantic duplicate
        if (input.SimilarAtom != null)
        {
            context.TraceActivity?.SetTag("atom.persisted", false);
            context.TraceActivity?.SetTag("atom.duplicate_type", "semantic");

            // Load embedding for similar atom
            var embedding = input.SimilarAtom.Embeddings?.FirstOrDefault();

            return new AtomIngestionPipelineResult
            {
                Atom = input.SimilarAtom,
                Embedding = embedding,
                WasDuplicate = true,
                DuplicateReason = $"Semantic similarity {input.SimilarityScore:F4}",
                SemanticSimilarity = input.SimilarityScore,
                TotalDuration = context.TraceActivity?.Duration ?? TimeSpan.Zero,
                CorrelationId = context.CorrelationId
            };
        }

        // Create new atom
        var atom = new Atom
        {
            ContentHash = input.ContentHash,
            Modality = input.Request.Modality,
            Subtype = input.Request.Subtype,
            SourceUri = input.Request.SourceUri,
            SourceType = input.Request.SourceType,
            CanonicalText = input.Request.CanonicalText,
            Metadata = input.Request.Metadata,
            PayloadLocator = input.Request.PayloadLocator,
            ReferenceCount = 1,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Add embedding if generated
        AtomEmbedding? newEmbedding = null;
        if (input.EmbeddingVector != null && input.EmbeddingVector.Length > 0)
        {
            var padded = VectorUtility.PadToSqlLength(input.EmbeddingVector, out var usedPadding);
            var sqlVector = padded.ToSqlVector();

            var spatialPoint = await _embeddingRepository
                .ComputeSpatialProjectionAsync(sqlVector, input.EmbeddingVector.Length, cancellationToken)
                .ConfigureAwait(false);

            newEmbedding = new AtomEmbedding
            {
                EmbeddingType = input.Request.EmbeddingType,
                Dimension = input.EmbeddingVector.Length,
                ModelId = input.Request.ModelId,
                SpatialGeometry = spatialPoint,
                EmbeddingVector = sqlVector,
                UsesMaxDimensionPadding = usedPadding
            };

            atom.Embeddings.Add(newEmbedding);
        }

        // Persist to database
        var savedAtom = await _atomRepository.AddAsync(atom, cancellationToken).ConfigureAwait(false);

        context.TraceActivity?.SetTag("atom.persisted", true);
        context.TraceActivity?.SetTag("atom.id", savedAtom.AtomId);
        context.TraceActivity?.SetTag("atom.has_embedding", newEmbedding != null);

        _logger.LogInformation(
            "Created new atom {AtomId} ({Modality}) with reference count 1. CorrelationId: {CorrelationId}",
            savedAtom.AtomId,
            savedAtom.Modality,
            context.CorrelationId);

        return new AtomIngestionPipelineResult
        {
            Atom = savedAtom,
            Embedding = newEmbedding,
            WasDuplicate = false,
            DuplicateReason = null,
            SemanticSimilarity = null,
            TotalDuration = context.TraceActivity?.Duration ?? TimeSpan.Zero,
            CorrelationId = context.CorrelationId
        };
    }
}

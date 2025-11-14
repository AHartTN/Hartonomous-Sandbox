using System;
using System.Collections.Generic;
using System.Linq;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Utilities;
using Microsoft.Data.SqlTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Default implementation for atom ingestion with hash-based deduplication and optional embedding capture.
/// </summary>
public class AtomIngestionService : IAtomIngestionService
{
    /// <summary>
    /// Repository used to persist and query atoms from durable storage.
    /// </summary>
    private readonly IAtomRepository _atomRepository;

    /// <summary>
    /// Repository that manages embeddings associated with stored atoms.
    /// </summary>
    private readonly IAtomEmbeddingRepository _atomEmbeddingRepository;

    /// <summary>
    /// Repository responsible for loading active deduplication policies.
    /// </summary>
    private readonly IDeduplicationPolicyRepository _policyRepository;

    /// <summary>
    /// Logger for operational diagnostics and duplicate detection traces.
    /// </summary>
    private readonly ILogger<AtomIngestionService> _logger;

    /// <summary>
    /// Default policy name pulled from configuration when requests omit one.
    /// </summary>
    private readonly string _defaultPolicyName;

    /// <summary>
    /// Creates a new ingestion service that performs deduplication and embedding persistence.
    /// </summary>
    /// <param name="atomRepository">Repository that manages atom entities.</param>
    /// <param name="atomEmbeddingRepository">Repository that handles atom embeddings and similarity lookups.</param>
    /// <param name="policyRepository">Repository providing deduplication policies.</param>
    /// <param name="logger">Structured logger for recording ingestion events.</param>
    /// <param name="configuration">Application configuration used to resolve defaults.</param>
    public AtomIngestionService(
        IAtomRepository atomRepository,
        IAtomEmbeddingRepository atomEmbeddingRepository,
        IDeduplicationPolicyRepository policyRepository,
        ILogger<AtomIngestionService> logger,
        IConfiguration configuration)
    {
        _atomRepository = atomRepository ?? throw new ArgumentNullException(nameof(atomRepository));
        _atomEmbeddingRepository = atomEmbeddingRepository ?? throw new ArgumentNullException(nameof(atomEmbeddingRepository));
        _policyRepository = policyRepository ?? throw new ArgumentNullException(nameof(policyRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultPolicyName = configuration.GetValue<string>("AtomIngestion:PolicyName", "default");
    }

    /// <summary>
    /// Ingests an atom payload, applying exact and semantic deduplication before persisting.
    /// </summary>
    /// <param name="request">Details about the atom, payload, embeddings, and policy overrides.</param>
    /// <param name="cancellationToken">Token for cancelling long-running database or similarity queries.</param>
    /// <returns>Information about the stored atom and whether it was considered a duplicate.</returns>
    public async Task<AtomIngestionResult> IngestAsync(AtomIngestionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.HashInput))
        {
            throw new ArgumentException("HashInput must be provided", nameof(request));
        }

        var contentHash = HashUtility.ComputeSHA256Bytes(request.HashInput);
        var existing = await _atomRepository.GetByContentHashAsync(contentHash, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            await _atomRepository.IncrementReferenceCountAsync(existing.AtomId, 1, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Atom duplicate detected by hash (atomId={AtomId}, modality={Modality})", existing.AtomId, existing.Modality);

            var matchingEmbedding = SelectMatchingEmbedding(existing.Embeddings, request.EmbeddingType, request.ModelId);

            return new AtomIngestionResult
            {
                Atom = existing,
                Embedding = matchingEmbedding,
                WasDuplicate = true,
                DuplicateReason = "Exact content hash match",
                SemanticSimilarity = null
            };
        }

        var policyName = string.IsNullOrWhiteSpace(request.PolicyName) ? _defaultPolicyName : request.PolicyName;
        var policy = await _policyRepository.GetActivePolicyAsync(policyName, cancellationToken).ConfigureAwait(false);
        if (policy is null)
        {
            _logger.LogWarning("Deduplication policy '{PolicyName}' not found. Proceeding without semantic deduplication.", policyName);
        }

        float[]? embedding = request.Embedding;
        var hasVector = false;
        var usedPadding = false;
        SqlVector<float> sqlVector = default;

        if (embedding is { Length: > 0 } && policy?.SemanticThreshold is double semanticThreshold)
        {
            var padded = VectorUtility.PadToSqlLength(embedding, out usedPadding);
            sqlVector = padded.ToSqlVector();
            hasVector = true;

            var similarityThreshold = Math.Clamp(semanticThreshold, -1d, 1d);
            var maxCosineDistance = Math.Max(0d, 1d - similarityThreshold);

            if (maxCosineDistance < 2d)
            {
                var semanticDuplicate = await _atomEmbeddingRepository
                    .FindNearestBySimilarityAsync(sqlVector, request.EmbeddingType, request.ModelId, maxCosineDistance, cancellationToken)
                    .ConfigureAwait(false);

                if (semanticDuplicate is not null)
                {
                    var duplicateAtom = semanticDuplicate.Embedding.Atom
                        ?? await _atomRepository.GetByIdAsync(semanticDuplicate.Embedding.AtomId, cancellationToken).ConfigureAwait(false)
                        ?? throw new InvalidOperationException($"Failed to load atom {semanticDuplicate.Embedding.AtomId} for semantic duplicate.");

                    await _atomRepository.IncrementReferenceCountAsync(duplicateAtom.AtomId, 1, cancellationToken).ConfigureAwait(false);

                    var similarity = 1d - semanticDuplicate.CosineDistance;

                    _logger.LogInformation(
                        "Atom duplicate detected by semantic similarity (atomId={AtomId}, similarity={Similarity:F4}, threshold={Threshold:F4})",
                        duplicateAtom.AtomId,
                        similarity,
                        similarityThreshold);

                    return new AtomIngestionResult
                    {
                        Atom = duplicateAtom,
                        Embedding = semanticDuplicate.Embedding,
                        WasDuplicate = true,
                        DuplicateReason = $"Semantic similarity {similarity:F4} ≥ {similarityThreshold:F4}",
                        SemanticSimilarity = similarity
                    };
                }
            }
        }

        if (!hasVector && embedding is { Length: > 0 })
        {
            var padded = VectorUtility.PadToSqlLength(embedding, out usedPadding);
            sqlVector = padded.ToSqlVector();
            hasVector = true;
        }

        var atom = new Atom
        {
            ContentHash = contentHash,
            Modality = request.Modality,
            Subtype = request.Subtype,
            SourceUri = request.SourceUri,
            SourceType = request.SourceType,
            CanonicalText = request.CanonicalText,
            Metadata = request.Metadata,
            PayloadLocator = request.PayloadLocator,
            ReferenceCount = 1,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        if (request.Components is { Count: > 0 })
        {
            atom.ComponentStream = ComponentStreamEncoder.Encode(request.Components);
        }

        AtomEmbedding? newEmbedding = null;
        if (embedding is { Length: > 0 } && hasVector)
        {
            var spatialPoint = await _atomEmbeddingRepository
                .ComputeSpatialProjectionAsync(sqlVector, embedding.Length, cancellationToken)
                .ConfigureAwait(false);
            var coarsePoint = CreateCoarsePoint(spatialPoint);

            var hasZ = spatialPoint.CoordinateSequence.HasZ;
            var rawX = spatialPoint.X;
            var rawY = spatialPoint.Y;
            var rawZ = hasZ ? spatialPoint.Z : 0d;
            const int NoZBucket = int.MinValue;

            newEmbedding = new AtomEmbedding
            {
                EmbeddingType = request.EmbeddingType,
                Dimension = embedding.Length,
                ModelId = request.ModelId,
                SpatialGeometry = spatialPoint,
                SpatialCoarse = coarsePoint,
                SpatialProjX = rawX,
                SpatialProjY = rawY,
                SpatialProjZ = hasZ ? rawZ : null,
                SpatialBucketX = (int)Math.Round(rawX, 0, MidpointRounding.ToZero),
                SpatialBucketY = (int)Math.Round(rawY, 0, MidpointRounding.ToZero),
                SpatialBucketZ = hasZ ? (int)Math.Round(rawZ, 0, MidpointRounding.ToZero) : NoZBucket,
                EmbeddingVector = sqlVector,
                UsesMaxDimensionPadding = usedPadding,
                Metadata = policy is null
                    ? null
                    : $"{{\"semanticThreshold\":{policy.SemanticThreshold?.ToString("F2") ?? "null"},\"policy\":\"{policy.PolicyName}\"}}"
            };

            atom.Embeddings.Add(newEmbedding);
        }

        var savedAtom = await _atomRepository.AddAsync(atom, cancellationToken).ConfigureAwait(false);

        if (newEmbedding is not null && newEmbedding.EmbeddingVector is null && embedding is { Length: > 0 })
        {
            var components = embedding!
                .Select((value, index) => new AtomEmbeddingComponent
                {
                    AtomEmbeddingId = newEmbedding.AtomEmbeddingId,
                    ComponentIndex = index,
                    ComponentValue = value
                })
                .ToList();

            if (components.Count > 0)
            {
                await _atomEmbeddingRepository.AddComponentsAsync(newEmbedding.AtomEmbeddingId, components, cancellationToken);
            }
        }

        _logger.LogInformation("Created new atom {AtomId} ({Modality}) with reference count 1", savedAtom.AtomId, savedAtom.Modality);

        if (newEmbedding is not null)
        {
            await _atomEmbeddingRepository
                .UpdateSpatialMetadataAsync(newEmbedding.AtomEmbeddingId, cancellationToken)
                .ConfigureAwait(false);
        }

        return new AtomIngestionResult
        {
            Atom = savedAtom,
            Embedding = newEmbedding,
            WasDuplicate = false,
            DuplicateReason = null,
            SemanticSimilarity = null
        };
    }

    /// <summary>
    /// Picks an embedding from a collection that matches the requested type and model constraints.
    /// </summary>
    /// <param name="embeddings">Embeddings associated with an atom.</param>
    /// <param name="embeddingType">Optional embedding modality filter.</param>
    /// <param name="modelId">Optional model identifier to match.</param>
    /// <returns>The first embedding that satisfies the filters; otherwise <see langword="null"/>.</returns>
    private static AtomEmbedding? SelectMatchingEmbedding(IEnumerable<AtomEmbedding> embeddings, string embeddingType, int? modelId)
    {
        if (embeddings is null)
        {
            return null;
        }

        var query = embeddings.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(embeddingType))
        {
            query = query.Where(e => string.Equals(e.EmbeddingType, embeddingType, StringComparison.OrdinalIgnoreCase));
        }

        if (modelId.HasValue)
        {
            query = query.Where(e => e.ModelId == modelId);
        }

        return query.FirstOrDefault();
    }

    /// <summary>
    /// Produces a coarse spatial approximation by rounding coordinates to whole units.
    /// </summary>
    /// <param name="source">Fine-grained spatial point.</param>
    /// <returns>Rounded point suitable for spatial bucketing.</returns>
    private static Point CreateCoarsePoint(Point source)
    {
        var coarseX = Math.Round(source.X, 0, MidpointRounding.ToZero);
        var coarseY = Math.Round(source.Y, 0, MidpointRounding.ToZero);
        var coarseZ = source.CoordinateSequence.HasZ ? Math.Round(source.Z, 0, MidpointRounding.ToZero) : 0d;
        var coordinate = source.CoordinateSequence.HasZ
            ? new CoordinateZ(coarseX, coarseY, coarseZ)
            : new Coordinate(coarseX, coarseY);
        return new Point(coordinate) { SRID = source.SRID };
    }
}

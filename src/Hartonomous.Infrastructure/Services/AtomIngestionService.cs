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

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Default implementation for atom ingestion with hash-based deduplication and optional embedding capture.
/// </summary>
public class AtomIngestionService : IAtomIngestionService
{
    private readonly IAtomRepository _atomRepository;
    private readonly IAtomEmbeddingRepository _atomEmbeddingRepository;
    private readonly IDeduplicationPolicyRepository _policyRepository;
    private readonly ILogger<AtomIngestionService> _logger;
    private readonly string _defaultPolicyName;

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
            sqlVector = new SqlVector<float>(padded);
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
            sqlVector = new SqlVector<float>(padded);
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

        AtomEmbedding? newEmbedding = null;
        if (embedding is { Length: > 0 } && hasVector)
        {
            var spatialPoint = await _atomEmbeddingRepository
                .ComputeSpatialProjectionAsync(sqlVector, embedding.Length, cancellationToken)
                .ConfigureAwait(false);
            var coarsePoint = CreateCoarsePoint(spatialPoint);

            newEmbedding = new AtomEmbedding
            {
                EmbeddingType = request.EmbeddingType,
                Dimension = embedding.Length,
                ModelId = request.ModelId,
                SpatialGeometry = spatialPoint,
                SpatialCoarse = coarsePoint,
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

        return new AtomIngestionResult
        {
            Atom = savedAtom,
            Embedding = newEmbedding,
            WasDuplicate = false,
            DuplicateReason = null,
            SemanticSimilarity = null
        };
    }

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

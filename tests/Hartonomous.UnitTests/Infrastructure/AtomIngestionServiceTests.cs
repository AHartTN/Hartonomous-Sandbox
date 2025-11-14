using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Enums;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Models;
using Hartonomous.Data.Entities;
using Hartonomous.Infrastructure.Services;
using Microsoft.Data.SqlTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;

namespace Hartonomous.UnitTests.Infrastructure;

public class AtomIngestionServiceTests
{
    [Fact]
    public async Task IngestAsync_NewAtom_PersistsAtomAndEmbedding()
    {
        var atomRepository = new StubAtomRepository();
        var embeddingRepository = new StubAtomEmbeddingRepository();
        var policyRepository = new StubPolicyRepository(new DeduplicationPolicy
        {
            PolicyName = "default",
            SemanticThreshold = 0.95,
            IsActive = true
        });

        var service = CreateService(atomRepository, embeddingRepository, policyRepository);

        var result = await service.IngestAsync(new AtomIngestionRequest
        {
            HashInput = "alpha",
            Modality = Modality.Text.ToJsonString(),
            Subtype = "document",
            SourceType = "test",
            CanonicalText = "alpha",
            EmbeddingType = "test",
            Embedding = new[] { 0.1f, 0.2f, 0.3f }
        });

        Assert.False(result.WasDuplicate);
        Assert.NotNull(result.Atom);
        Assert.NotNull(result.Embedding);
        Assert.Single(atomRepository.StoredAtoms);
        Assert.Equal(1, atomRepository.StoredAtoms[0].ReferenceCount);
    }

    [Fact]
    public async Task IngestAsync_DuplicateHash_IncrementsReferenceCount()
    {
        var atomRepository = new StubAtomRepository();
        var embeddingRepository = new StubAtomEmbeddingRepository();
        var policyRepository = new StubPolicyRepository(new DeduplicationPolicy
        {
            PolicyName = "default",
            SemanticThreshold = 0.95,
            IsActive = true
        });

        var service = CreateService(atomRepository, embeddingRepository, policyRepository);

        var request = new AtomIngestionRequest
        {
            HashInput = "duplicate",
            Modality = Modality.Text.ToJsonString(),
            Subtype = "document",
            SourceType = "test",
            CanonicalText = "duplicate",
            EmbeddingType = "test",
            Embedding = new[] { 0.2f, 0.3f, 0.4f }
        };

        var first = await service.IngestAsync(request);
        var second = await service.IngestAsync(request);

        Assert.False(first.WasDuplicate);
        Assert.True(second.WasDuplicate);
        Assert.Equal(2, atomRepository.StoredAtoms.Single().ReferenceCount);
    }

    [Fact]
    public async Task IngestAsync_SemanticDuplicate_UsesSimilarityResult()
    {
        var atomRepository = new StubAtomRepository();
        var embeddingRepository = new StubAtomEmbeddingRepository();
        var policyRepository = new StubPolicyRepository(new DeduplicationPolicy
        {
            PolicyName = "default",
            SemanticThreshold = 0.9,
            IsActive = true
        });

        var service = CreateService(atomRepository, embeddingRepository, policyRepository);

        var seed = await service.IngestAsync(new AtomIngestionRequest
        {
            HashInput = "seed",
            Modality = Modality.Text.ToJsonString(),
            Subtype = "document",
            SourceType = "test",
            CanonicalText = "seed",
            EmbeddingType = "test",
            Embedding = new[] { 0.3f, 0.4f, 0.5f }
        });

        var duplicateAtom = seed.Atom;
        Assert.NotNull(duplicateAtom);
        Assert.NotNull(seed.Embedding);

        embeddingRepository.NextSimilarityResult = new AtomEmbeddingSearchResult
        {
            Embedding = new AtomEmbedding
            {
                AtomEmbeddingId = seed.Embedding!.AtomEmbeddingId,
                AtomId = duplicateAtom!.AtomId,
                Atom = duplicateAtom,
                EmbeddingType = "test",
                Dimension = seed.Embedding.Dimension,
                EmbeddingVector = seed.Embedding.EmbeddingVector
            },
            CosineDistance = 0.05,
            SpatialDistance = double.NaN
        };

        var result = await service.IngestAsync(new AtomIngestionRequest
        {
            HashInput = "different",
            Modality = Modality.Text.ToJsonString(),
            Subtype = "document",
            SourceType = "test",
            CanonicalText = "different",
            EmbeddingType = "test",
            Embedding = new[] { 0.31f, 0.39f, 0.49f }
        });

        Assert.True(result.WasDuplicate);
        Assert.Equal(duplicateAtom!.AtomId, result.Atom.AtomId);
        Assert.Equal(2, duplicateAtom.ReferenceCount);
        Assert.Equal("Semantic similarity 0.9500 ≥ 0.9000", result.DuplicateReason);
    }

    private static AtomIngestionService CreateService(
        IAtomRepository atomRepository,
        IAtomEmbeddingRepository embeddingRepository,
        IDeduplicationPolicyRepository policyRepository)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AtomIngestion:PolicyName"] = "default",
                ["AtomIngestion:DefaultModality"] = "text"
            })
            .Build();

        return new AtomIngestionService(
            atomRepository,
            embeddingRepository,
            policyRepository,
            NullLogger<AtomIngestionService>.Instance,
            configuration);
    }

    private sealed class StubAtomRepository : IAtomRepository
    {
        private long _nextId = 0;
        public List<Atom> StoredAtoms { get; } = new();

        public Task<Atom> AddAsync(Atom atom, CancellationToken cancellationToken = default)
        {
            atom.AtomId = Interlocked.Increment(ref _nextId);
            foreach (var embedding in atom.AtomEmbeddings)
            {
                embedding.AtomId = atom.AtomId;
                embedding.AtomEmbeddingId = embedding.AtomEmbeddingId == 0
                    ? Interlocked.Increment(ref StubAtomEmbeddingRepository.GlobalEmbeddingId)
                    : embedding.AtomEmbeddingId;
            }

            StoredAtoms.Add(atom);
            return Task.FromResult(atom);
        }

        public Task<Atom?> GetByContentHashAsync(byte[] contentHash, CancellationToken cancellationToken = default)
            => Task.FromResult(StoredAtoms.FirstOrDefault(a => a.ContentHash.SequenceEqual(contentHash)));

        public Task<Atom?> GetByIdAsync(long atomId, CancellationToken cancellationToken = default)
            => Task.FromResult(StoredAtoms.FirstOrDefault(a => a.AtomId == atomId));

        public Task IncrementReferenceCountAsync(long atomId, long delta = 1, CancellationToken cancellationToken = default)
        {
            var atom = StoredAtoms.FirstOrDefault(a => a.AtomId == atomId);
            if (atom != null)
            {
                atom.ReferenceCount += delta;
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Atom>> GetByModalityAsync(string modality, int take = 100, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Atom>>(StoredAtoms.Where(a => a.Modality == modality).Take(take).ToList());

        public Task UpdateMetadataAsync(long atomId, string? metadata, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateSpatialKeyAsync(long atomId, Point spatialKey, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(long atomId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubAtomEmbeddingRepository : IAtomEmbeddingRepository
    {
        public static long GlobalEmbeddingId;

        public AtomEmbeddingSearchResult? NextSimilarityResult { get; set; }
        public ConcurrentDictionary<long, AtomEmbedding> Embeddings { get; } = new();

        public Task<AtomEmbedding> AddAsync(AtomEmbedding embedding, CancellationToken cancellationToken = default)
        {
            embedding.AtomEmbeddingId = Interlocked.Increment(ref GlobalEmbeddingId);
            Embeddings[embedding.AtomEmbeddingId] = embedding;
            return Task.FromResult(embedding);
        }

        public Task AddComponentsAsync(long atomEmbeddingId, IEnumerable<AtomEmbeddingComponent> components, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Point> ComputeSpatialProjectionAsync(SqlVector<float> paddedVector, int originalDimension, CancellationToken cancellationToken = default)
        {
            var span = paddedVector.Memory.Span;
            var x = span.Length > 0 ? span[0] : 0f;
            var y = span.Length > 1 ? span[1] : 0f;
            var z = span.Length > 2 ? span[2] : 0f;
            return Task.FromResult<Point>(new Point(new CoordinateZ(x, y, z)) { SRID = 0 });
        }

        public Task<AtomEmbedding?> GetByIdAsync(long atomEmbeddingId, CancellationToken cancellationToken = default)
        {
            Embeddings.TryGetValue(atomEmbeddingId, out var embedding);
            return Task.FromResult(embedding);
        }

        public Task<IReadOnlyList<AtomEmbedding>> GetByAtomIdAsync(long atomId, CancellationToken cancellationToken = default)
        {
            var matches = Embeddings.Values.Where(e => e.AtomId == atomId).ToList();
            return Task.FromResult<IReadOnlyList<AtomEmbedding>>(matches);
        }

        public Task<AtomEmbeddingSearchResult?> FindNearestBySimilarityAsync(SqlVector<float> paddedVector, string embeddingType, int? modelId, double maxCosineDistance, CancellationToken cancellationToken = default)
        {
            if (NextSimilarityResult is null)
            {
                return Task.FromResult<AtomEmbeddingSearchResult?>(null);
            }

            return Task.FromResult(NextSimilarityResult.CosineDistance <= maxCosineDistance
                ? NextSimilarityResult
                : null);
        }

        public Task<IReadOnlyList<AtomEmbeddingSearchResult>> HybridSearchAsync(float[] vector, Point spatial3D, int spatialCandidates, int finalTopK, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AtomEmbeddingSearchResult>>(Array.Empty<AtomEmbeddingSearchResult>());

        public Task UpdateSpatialMetadataAsync(long atomEmbeddingId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubPolicyRepository : IDeduplicationPolicyRepository
    {
        private DeduplicationPolicy _policy;

        public StubPolicyRepository(DeduplicationPolicy policy)
        {
            _policy = policy;
        }

        public Task<DeduplicationPolicy> AddAsync(DeduplicationPolicy policy, CancellationToken cancellationToken = default)
        {
            _policy = policy;
            return Task.FromResult(_policy);
        }

        public Task<IReadOnlyList<DeduplicationPolicy>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DeduplicationPolicy>>(new[] { _policy });

        public Task<DeduplicationPolicy?> GetActivePolicyAsync(string policyName, CancellationToken cancellationToken = default)
            => Task.FromResult(_policy.IsActive && string.Equals(policyName, _policy.PolicyName, StringComparison.OrdinalIgnoreCase) ? _policy : null);

        public Task UpdateAsync(DeduplicationPolicy policy, CancellationToken cancellationToken = default)
        {
            _policy = policy;
            return Task.CompletedTask;
        }
    }
}


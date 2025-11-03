using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Models;
using Hartonomous.Testing.Common;
using Microsoft.Data.SqlTypes;
using Microsoft.Extensions.Logging;
using ModelIngestion;
using NetTopologySuite.Geometries;

namespace Hartonomous.UnitTests.ModelIngestion;

public sealed class QueryServiceTests
{
    [Fact]
    public async Task ExecuteSemanticQueryAsync_LogsMatchesWhenResultsReturned()
    {
        var repository = new StubAtomEmbeddingRepository(hasResults: true);
        var logger = TestLogger.Create<QueryService>();
        var service = new QueryService(repository, logger);

        await service.ExecuteSemanticQueryAsync("test query", CancellationToken.None);

        Assert.True(repository.ComputeSpatialProjectionCalls > 0);
        Assert.True(repository.HybridSearchCalls > 0);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Information && entry.Message.Contains("Top", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteSemanticQueryAsync_LogsWarningWhenNoMatches()
    {
        var repository = new StubAtomEmbeddingRepository(hasResults: false);
        var logger = TestLogger.Create<QueryService>();
        var service = new QueryService(repository, logger);

        await service.ExecuteSemanticQueryAsync("another query", CancellationToken.None);

        Assert.True(repository.ComputeSpatialProjectionCalls > 0);
        Assert.True(repository.HybridSearchCalls > 0);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("No atom embeddings matched", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class StubAtomEmbeddingRepository : IAtomEmbeddingRepository
    {
        private readonly bool _hasResults;

        public StubAtomEmbeddingRepository(bool hasResults)
        {
            _hasResults = hasResults;
        }

        public int ComputeSpatialProjectionCalls { get; private set; }
        public int HybridSearchCalls { get; private set; }

        public Task<AtomEmbedding?> GetByIdAsync(long atomEmbeddingId, CancellationToken cancellationToken = default) => Task.FromResult<AtomEmbedding?>(null);

        public Task<IReadOnlyList<AtomEmbedding>> GetByAtomIdAsync(long atomId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AtomEmbedding>>(Array.Empty<AtomEmbedding>());

        public Task<AtomEmbedding> AddAsync(AtomEmbedding embedding, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task AddComponentsAsync(long atomEmbeddingId, IEnumerable<AtomEmbeddingComponent> components, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Point> ComputeSpatialProjectionAsync(SqlVector<float> paddedVector, int originalDimension, CancellationToken cancellationToken = default)
        {
            ComputeSpatialProjectionCalls++;
            return Task.FromResult<Point>(new Point(new CoordinateZ(0, 0, 0)));
        }

        public Task<AtomEmbeddingSearchResult?> FindNearestBySimilarityAsync(SqlVector<float> paddedVector, string embeddingType, int? modelId, double maxCosineDistance, CancellationToken cancellationToken = default)
            => Task.FromResult<AtomEmbeddingSearchResult?>(null);

        public Task<IReadOnlyList<AtomEmbeddingSearchResult>> HybridSearchAsync(float[] vector, Point spatial3D, int spatialCandidates, int finalTopK, CancellationToken cancellationToken = default)
        {
            HybridSearchCalls++;

            if (!_hasResults)
            {
                return Task.FromResult<IReadOnlyList<AtomEmbeddingSearchResult>>(Array.Empty<AtomEmbeddingSearchResult>());
            }

            var atom = new Atom { AtomId = 1, ContentHash = Array.Empty<byte>(), Modality = "text", CanonicalText = "Sample" };
            var embedding = new AtomEmbedding { AtomEmbeddingId = 1, AtomId = 1, Atom = atom, EmbeddingType = "test" };
            atom.Embeddings.Add(embedding);

            IReadOnlyList<AtomEmbeddingSearchResult> results = new[]
            {
                new AtomEmbeddingSearchResult { Embedding = embedding, CosineDistance = 0.1, SpatialDistance = 0.2 }
            };

            return Task.FromResult(results);
        }
    }
}

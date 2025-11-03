using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Services;
using Hartonomous.Testing.Common;
using Microsoft.Extensions.Configuration;

namespace Hartonomous.UnitTests.ModelIngestion;

public sealed class EmbeddingTestServiceTests
{
    [Fact]
    public async Task IngestSampleEmbeddingsAsync_InvokesEmbeddingServiceForEachRequest()
    {
        var atomService = new TrackingAtomIngestionService();
        var ingestionService = CreateEmbeddingIngestionService(atomService);
        var logger = TestLogger.Silent<global::ModelIngestion.EmbeddingTestService>();
        var service = new global::ModelIngestion.EmbeddingTestService(ingestionService, logger);

        await service.IngestSampleEmbeddingsAsync(5, CancellationToken.None);

        Assert.Equal(5, atomService.Requests.Count);
        Assert.All(atomService.Requests, request => Assert.Equal(768, request.Embedding!.Length));
    }

    [Fact]
    public async Task TestDeduplicationAsync_ProducesExpectedDuplicatePatterns()
    {
        var atomService = new TrackingAtomIngestionService();
        var ingestionService = CreateEmbeddingIngestionService(atomService);
        var logger = TestLogger.Silent<global::ModelIngestion.EmbeddingTestService>();
        var service = new global::ModelIngestion.EmbeddingTestService(ingestionService, logger);

        await service.TestDeduplicationAsync(CancellationToken.None);

        Assert.Equal(3, atomService.Results.Count);
        Assert.False(atomService.Results[0].WasDuplicate);
        Assert.True(atomService.Results[1].WasDuplicate);
        Assert.True(atomService.Results[2].WasDuplicate);
        Assert.Equal("hash duplicate", atomService.Results[1].DuplicateReason);
        Assert.Equal("Semantic similarity 0.9500 ≥ 0.9000", atomService.Results[2].DuplicateReason);
    }

    private static global::ModelIngestion.EmbeddingIngestionService CreateEmbeddingIngestionService(TrackingAtomIngestionService atomService)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        return new global::ModelIngestion.EmbeddingIngestionService(
            atomService,
            TestLogger.Silent<global::ModelIngestion.EmbeddingIngestionService>(),
            configuration);
    }

    private sealed class TrackingAtomIngestionService : IAtomIngestionService
    {
        private readonly Dictionary<string, Atom> _atoms = new();

        public List<AtomIngestionRequest> Requests { get; } = new();
        public List<AtomIngestionResult> Results { get; } = new();

        public Task<AtomIngestionResult> IngestAsync(AtomIngestionRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);

            var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(request.HashInput)));
            var isDuplicate = _atoms.TryGetValue(hash, out var existingAtom);
            string? reason = null;
            double? similarity = null;

            if (request.CanonicalText?.Contains("semantically similar", StringComparison.OrdinalIgnoreCase) == true)
            {
                isDuplicate = true;
                existingAtom = _atoms.Values.FirstOrDefault() ?? CreateAtom(request, _atoms.Count + 1);
                reason = "Semantic similarity 0.9500 ≥ 0.9000";
                similarity = 0.95;
            }
            else if (!isDuplicate)
            {
                var atom = CreateAtom(request, _atoms.Count + 1);
                _atoms[hash] = atom;
                existingAtom = atom;
            }
            else if (request.CanonicalText != existingAtom!.CanonicalText)
            {
                reason = "Semantic similarity 0.9500 ≥ 0.9000";
                similarity = 0.95;
            }
            else
            {
                reason = "hash duplicate";
            }

            var embedding = new AtomEmbedding
            {
                AtomEmbeddingId = Results.Count + 1,
                AtomId = existingAtom!.AtomId,
                Atom = existingAtom,
                EmbeddingType = request.EmbeddingType,
                Dimension = request.Embedding?.Length ?? 0
            };

            var result = new AtomIngestionResult
            {
                Atom = existingAtom,
                Embedding = embedding,
                WasDuplicate = isDuplicate,
                DuplicateReason = reason,
                SemanticSimilarity = similarity
            };

            Results.Add(result);
            return Task.FromResult(result);
        }

        private static Atom CreateAtom(AtomIngestionRequest request, int atomId)
        {
            return new Atom
            {
                AtomId = atomId,
                ContentHash = SHA256.HashData(Encoding.UTF8.GetBytes(request.HashInput)),
                CanonicalText = request.CanonicalText,
                Modality = request.Modality
            };
        }
    }
}

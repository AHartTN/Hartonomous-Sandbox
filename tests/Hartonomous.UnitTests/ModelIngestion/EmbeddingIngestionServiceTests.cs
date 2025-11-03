using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Services;
using Hartonomous.Testing.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hartonomous.UnitTests.ModelIngestion;

public sealed class EmbeddingIngestionServiceTests
{
    [Fact]
    public async Task IngestEmbeddingAsync_PersistsEmbeddingAndLogsNewAtom()
    {
        var atomService = new StubAtomIngestionService();
        var logger = TestLogger.Create<global::ModelIngestion.EmbeddingIngestionService>();
        var configuration = BuildConfiguration(dimension: 3, policy: "default", modality: "text", model: "production");
        var service = new global::ModelIngestion.EmbeddingIngestionService(atomService, logger, configuration);

        var embedding = new[] { 0.1f, 0.2f, 0.3f };
        var result = await service.IngestEmbeddingAsync("hello world", "document", embedding, null, CancellationToken.None);

        var request = atomService.LastRequest;
        Assert.NotNull(request);
        Assert.Equal("hello world", request!.HashInput);
        Assert.Equal("document", request.Subtype);
        Assert.Same(embedding, request.Embedding);
        Assert.True(result.AtomId > 0);
        Assert.False(result.WasDuplicate);

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Information && entry.Message.Contains("Stored new atom", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task IngestEmbeddingAsync_LogsReuseWhenDuplicate()
    {
        var atomService = new StubAtomIngestionService
        {
            NextResult = new AtomIngestionResult
            {
                Atom = StubAtomIngestionService.CreateAtom("duplicate"),
                Embedding = StubAtomIngestionService.CreateEmbedding(),
                WasDuplicate = true,
                DuplicateReason = "semantic match",
                SemanticSimilarity = 0.94
            }
        };

        var logger = TestLogger.Create<global::ModelIngestion.EmbeddingIngestionService>();
        var configuration = BuildConfiguration(dimension: 3, policy: "default", modality: "text", model: "production");
        var service = new global::ModelIngestion.EmbeddingIngestionService(atomService, logger, configuration);

        await service.IngestEmbeddingAsync("duplicate", "doc", new[] { 0.1f, 0.2f, 0.3f });

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Information && entry.Message.Contains("Reused existing atom", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task IngestEmbeddingAsync_ThrowsWhenEmbeddingNull()
    {
        var service = CreateService(dimension: 3);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.IngestEmbeddingAsync("text", "type", null!));
    }

    [Fact]
    public async Task IngestEmbeddingAsync_ThrowsWhenDimensionMismatch()
    {
        var service = CreateService(dimension: 3);

        await Assert.ThrowsAsync<ArgumentException>(() => service.IngestEmbeddingAsync("text", "type", new[] { 0.1f, 0.2f }));
    }

    [Fact]
    public async Task IngestBatchAsync_ProcessesAllEntries()
    {
        var atomService = new StubAtomIngestionService();
        var configuration = BuildConfiguration(dimension: 3, policy: "default", modality: "text", model: "production");
        var service = new global::ModelIngestion.EmbeddingIngestionService(atomService, TestLogger.Silent<global::ModelIngestion.EmbeddingIngestionService>(), configuration);

        var batch = new List<(string sourceText, string sourceType, float[] embedding)>
        {
            ("a", "t", new[] { 0.1f, 0.2f, 0.3f }),
            ("b", "t", new[] { 0.2f, 0.3f, 0.4f }),
            ("c", "t", new[] { 0.3f, 0.4f, 0.5f })
        };

        var results = await service.IngestBatchAsync(batch);

        Assert.Equal(3, atomService.Requests.Count);
        Assert.Equal(3, results.Count());
    }

    private static global::ModelIngestion.EmbeddingIngestionService CreateService(int dimension)
    {
        var atomService = new StubAtomIngestionService();
        var logger = TestLogger.Silent<global::ModelIngestion.EmbeddingIngestionService>();
        var configuration = BuildConfiguration(dimension, policy: "default", modality: "text", model: "production");
        return new global::ModelIngestion.EmbeddingIngestionService(atomService, logger, configuration);
    }

    private static IConfiguration BuildConfiguration(int dimension, string policy, string modality, string model)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ingestion:EmbeddingModel"] = model,
                ["Ingestion:EmbeddingDimension"] = dimension.ToString(),
                ["Ingestion:DeduplicationPolicy"] = policy,
                ["Ingestion:DefaultModality"] = modality
            })
            .Build();
    }

    private sealed class StubAtomIngestionService : IAtomIngestionService
    {
        public List<AtomIngestionRequest> Requests { get; } = new();

        public AtomIngestionResult? NextResult { get; set; }
            = new AtomIngestionResult
            {
                Atom = CreateAtom("default"),
                Embedding = CreateEmbedding(),
                WasDuplicate = false,
                DuplicateReason = null,
                SemanticSimilarity = null
            };

        public AtomIngestionRequest? LastRequest => Requests.LastOrDefault();

        public Task<AtomIngestionResult> IngestAsync(AtomIngestionRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            var result = NextResult ?? throw new InvalidOperationException("No result configured.");
            NextResult = new AtomIngestionResult
            {
                Atom = CreateAtom(request.HashInput),
                Embedding = CreateEmbedding(),
                WasDuplicate = false
            };
            return Task.FromResult(result);
        }

        internal static Atom CreateAtom(string text)
        {
            return new Atom
            {
                AtomId = Math.Abs(text.GetHashCode()) + 1L,
                ContentHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)),
                CanonicalText = text,
                Modality = "text"
            };
        }

        internal static AtomEmbedding CreateEmbedding()
        {
            return new AtomEmbedding
            {
                AtomEmbeddingId = 1,
                AtomId = 1,
                EmbeddingType = "production",
                Atom = CreateAtom("embedded")
            };
        }
    }
}

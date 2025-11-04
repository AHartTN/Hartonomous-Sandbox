using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Enums;
using Hartonomous.Core.Interfaces;
using Hartonomous.Testing.Common;
using Microsoft.Data.SqlTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hartonomous.UnitTests.ModelIngestion;

public sealed class IngestionOrchestratorTests : IDisposable
{
    private readonly string _tempModelPath;

    public IngestionOrchestratorTests()
    {
        _tempModelPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".safetensors");
        File.WriteAllText(_tempModelPath, string.Empty);
    }

    [Fact]
    public async Task RunAsync_IngestModel_InvokesIngestionPipeline()
    {
        var logger = TestLogger.Create<global::ModelIngestion.IngestionOrchestrator>();
        var repository = new StubModelRepository();
        repository.LayersToReturn = new[]
        {
            new ModelLayer { LayerId = 1, ModelId = 42, LayerName = "dense", LayerType = "linear", ParameterCount = 128 }
        };

        var ingestion = new StubModelIngestionService { NextModelId = 42 };

        var orchestrator = CreateOrchestrator(logger, repository, ingestion);

        await orchestrator.RunAsync(new[] { "ingest-model", _tempModelPath });

        Assert.Equal(_tempModelPath, ingestion.LastModelPath);
        Assert.Equal(42, repository.LastRequestedModelId);
        Assert.Contains(logger.Entries, e => e.Message.Contains("Ingesting model", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(logger.Entries, e => e.Message.Contains("Model has 1 layers", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RunAsync_UnknownCommand_LogsErrorAndShowsUsage()
    {
        var logger = TestLogger.Create<global::ModelIngestion.IngestionOrchestrator>();
        var repository = new StubModelRepository();
        var ingestion = new StubModelIngestionService { NextModelId = 7 };

        using var writer = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(writer);

        try
        {
            var orchestrator = CreateOrchestrator(logger, repository, ingestion);
            await orchestrator.RunAsync(new[] { "unknown-command" });
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("Unknown command", StringComparison.Ordinal));
        Assert.Contains("Usage:", writer.ToString(), StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (File.Exists(_tempModelPath))
        {
            File.Delete(_tempModelPath);
        }
    }

    private static global::ModelIngestion.IngestionOrchestrator CreateOrchestrator(
        ILogger<global::ModelIngestion.IngestionOrchestrator> logger,
        IModelRepository repository,
        StubModelIngestionService ingestion)
    {
        return new global::ModelIngestion.IngestionOrchestrator(
            logger,
            repository,
            downloadService: null!,
            embeddingTestService: null!,
            queryService: null!,
            atomicTestService: null!,
            modelIngestion: ingestion,
            embeddingService: CreateEmbeddingIngestionService(),
            pixelRepository: new StubAtomicPixelRepository(),
            audioSampleRepository: new StubAtomicAudioRepository(),
            textTokenRepository: new StubAtomicTextRepository());
    }

    private static global::ModelIngestion.EmbeddingIngestionService CreateEmbeddingIngestionService()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ingestion:EmbeddingModel"] = "test",
            ["Ingestion:EmbeddingDimension"] = "3"
        }).Build();

        return new global::ModelIngestion.EmbeddingIngestionService(
            new StubAtomIngestionService(),
            TestLogger.Silent<global::ModelIngestion.EmbeddingIngestionService>(),
            configuration);
    }

    private sealed class StubModelRepository : IModelRepository
    {
        public int LastRequestedModelId { get; private set; }
        public IEnumerable<ModelLayer> LayersToReturn { get; set; } = Array.Empty<ModelLayer>();

        public Task<Model?> GetByIdAsync(int modelId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Model?>(new Model { ModelId = modelId, ModelName = "test", ModelType = "transformer" });

        public Task<Model?> GetByNameAsync(string modelName, CancellationToken cancellationToken = default) =>
            Task.FromResult<Model?>(null);

        public Task<IEnumerable<Model>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Model>>(Array.Empty<Model>());

        public Task<IEnumerable<Model>> GetByTypeAsync(string modelType, CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Model>>(Array.Empty<Model>());

        public Task<Model> AddAsync(Model model, CancellationToken cancellationToken = default) =>
            Task.FromResult(model);

        public Task UpdateAsync(Model model, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(int modelId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> ExistsAsync(int modelId, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<int> GetCountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<ModelLayer> AddLayerAsync(int modelId, ModelLayer layer, CancellationToken cancellationToken = default) => Task.FromResult(layer);

        public Task UpdateLayerWeightsAsync(int layerId, SqlVector<float> weights, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IEnumerable<ModelLayer>> GetLayersByModelIdAsync(int modelId, CancellationToken cancellationToken = default)
        {
            LastRequestedModelId = modelId;
            return Task.FromResult(LayersToReturn);
        }

        public Task<IEnumerable<Model>> GetModelsByCapabilityAsync(TaskType[] tasks, Modality requiredModalities = Modality.None, int minCount = 1, CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Model>>(Array.Empty<Model>());

        public Task<List<Model>> GetByIdsAsync(IReadOnlyList<int> modelIds, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<Model>());

        public Task<List<Model>> GetActiveModelsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<Model>());
    }

    private sealed class StubModelIngestionService : IModelIngestionService
    {
        public string ServiceName => "StubModelIngestion";
        public string? LastModelPath { get; private set; }
        public int NextModelId { get; set; } = 1;

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<int> IngestAsync(string modelPath, string? modelName = null, CancellationToken cancellationToken = default)
        {
            LastModelPath = modelPath;
            return Task.FromResult(NextModelId);
        }

        public Task<int[]> IngestDirectoryAsync(string directoryPath, string searchPattern = "*", CancellationToken cancellationToken = default) =>
            Task.FromResult(Array.Empty<int>());

        public Task<IngestionStats> GetStatsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new IngestionStats());
    }

    private sealed class StubAtomIngestionService : IAtomIngestionService
    {
        public Task<AtomIngestionResult> IngestAsync(AtomIngestionRequest request, CancellationToken cancellationToken = default)
        {
            var atom = new Atom
            {
                AtomId = 1,
                ContentHash = SHA256.HashData(Encoding.UTF8.GetBytes(request.HashInput)),
                CanonicalText = request.CanonicalText,
                Modality = request.Modality
            };

            var embedding = new AtomEmbedding
            {
                AtomEmbeddingId = 1,
                AtomId = atom.AtomId,
                Atom = atom,
                EmbeddingType = request.EmbeddingType,
                Dimension = request.Embedding?.Length ?? 0
            };

            atom.Embeddings.Add(embedding);
            return Task.FromResult(new AtomIngestionResult
            {
                Atom = atom,
                Embedding = embedding,
                WasDuplicate = false,
                DuplicateReason = null,
                SemanticSimilarity = null
            });
        }
    }

    private sealed class StubAtomicPixelRepository : IAtomicPixelRepository
    {
        public Task<AtomicPixel?> GetByHashAsync(byte[] pixelHash, CancellationToken cancellationToken = default) => Task.FromResult<AtomicPixel?>(null);
        public Task<AtomicPixel> AddAsync(AtomicPixel pixel, CancellationToken cancellationToken = default) => Task.FromResult(pixel);
        public Task UpdateReferenceCountAsync(byte[] pixelHash, long delta = 1, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<long> GetReferenceCountAsync(byte[] pixelHash, CancellationToken cancellationToken = default) => Task.FromResult(0L);
    }

    private sealed class StubAtomicAudioRepository : IAtomicAudioSampleRepository
    {
        public Task<AtomicAudioSample?> GetByHashAsync(byte[] hash, CancellationToken cancellationToken = default) => Task.FromResult<AtomicAudioSample?>(null);
        public Task<AtomicAudioSample> AddAsync(AtomicAudioSample sample, CancellationToken cancellationToken = default) => Task.FromResult(sample);
        public Task UpdateReferenceCountAsync(byte[] hash, long delta = 1, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<long> GetReferenceCountAsync(byte[] hash, CancellationToken cancellationToken = default) => Task.FromResult(0L);
    }

    private sealed class StubAtomicTextRepository : IAtomicTextTokenRepository
    {
        public Task<AtomicTextToken?> GetByHashAsync(byte[] tokenHash, CancellationToken cancellationToken = default) => Task.FromResult<AtomicTextToken?>(null);
        public Task<AtomicTextToken> AddAsync(AtomicTextToken token, CancellationToken cancellationToken = default) => Task.FromResult(token);
        public Task UpdateReferenceCountAsync(long tokenId, long delta = 1, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<long> GetReferenceCountAsync(long tokenId, CancellationToken cancellationToken = default) => Task.FromResult(0L);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Enums;
using Hartonomous.Data.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Services;
using Hartonomous.Testing.Common;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Hartonomous.UnitTests.Infrastructure;

public sealed class ModelIngestionProcessorTests
{
    [Fact]
    public async Task ProcessAsync_PersistsModelAndLayers()
    {
        var modelToIngest = CreateModel("Original");
        var orchestrator = CreateOrchestrator(modelToIngest);
        var modelRepository = new StubModelRepository
        {
            ResultToReturn = new Model { ModelId = 99, ModelName = "Original", ModelType = "LLM" }
        };
        var layerRepository = new StubModelLayerRepository();
        var atomRepository = new StubAtomRepository();
        var relationRepository = new StubAtomRelationRepository();
        var processor = new ModelIngestionProcessor(
            TestLogger.Create<ModelIngestionProcessor>(),
            modelRepository,
            layerRepository,
            atomRepository,
            relationRepository,
            orchestrator);

        var request = new ModelIngestionRequest { ModelPath = "C:/models/model.onnx" };
        var result = await processor.ProcessAsync(request);

        Assert.True(result.Success);
        Assert.Equal(99, result.ModelId);
        Assert.Same(modelRepository.ResultToReturn, result.Model);
        Assert.Equal("Original", modelRepository.LastSavedModel?.ModelName);
        Assert.True(layerRepository.BulkInsertInvoked);
        Assert.All(layerRepository.LastLayers!, layer => Assert.Equal(99, layer.ModelId));
        Assert.Equal(layerRepository.LastLayers!.Count, atomRepository.AddedAtoms.Count);
        Assert.All(layerRepository.LastLayers!, layer => Assert.NotNull(layer.LayerAtomId));

        var orderedLayers = layerRepository.LastLayers!
            .OrderBy(l => l.LayerIdx)
            .ToList();

        if (orderedLayers.Count > 1)
        {
            Assert.Equal(orderedLayers.Count - 1, relationRepository.AddedRelations.Count);
            for (var i = 0; i < orderedLayers.Count - 1; i++)
            {
                var sourceAtomId = orderedLayers[i].LayerAtomId;
                var targetAtomId = orderedLayers[i + 1].LayerAtomId;
                var relation = relationRepository.AddedRelations[i];
                Assert.Equal(sourceAtomId, relation.SourceAtomId);
                Assert.Equal(targetAtomId, relation.TargetAtomId);
                Assert.Equal("architecture.successor", relation.RelationType);
            }
        }
    }

    [Fact]
    public async Task ProcessAsync_WhenCustomNameProvided_OverridesModelName()
    {
        var modelToIngest = CreateModel("Original");
        var orchestrator = CreateOrchestrator(modelToIngest);
        var modelRepository = new StubModelRepository
        {
            ResultToReturn = new Model { ModelId = 7, ModelName = "Persisted", ModelType = "LLM" }
        };
        var layerRepository = new StubModelLayerRepository();
        var atomRepository = new StubAtomRepository();
        var relationRepository = new StubAtomRelationRepository();
        var processor = new ModelIngestionProcessor(
            TestLogger.Create<ModelIngestionProcessor>(),
            modelRepository,
            layerRepository,
            atomRepository,
            relationRepository,
            orchestrator);

        var request = new ModelIngestionRequest
        {
            ModelPath = "C:/models/model.onnx",
            CustomName = "Override"
        };

        var result = await processor.ProcessAsync(request);

        Assert.True(result.Success);
        Assert.Equal(7, result.ModelId);
        Assert.Equal("Override", modelRepository.LastSavedModel?.ModelName);
    }

    [Fact]
    public async Task ProcessAsync_WhenIngestionFails_ReturnsFailureAndLogsError()
    {
        var failingReader = new StubOnnxReader
        {
            ValidateResult = false
        };
        var orchestrator = CreateOrchestrator(new Model { ModelName = "ShouldNotPersist", ModelType = "LLM" }, failingReader);
        var modelRepository = new StubModelRepository();
        var layerRepository = new StubModelLayerRepository();
        var atomRepository = new StubAtomRepository();
        var relationRepository = new StubAtomRelationRepository();
        var logger = TestLogger.Create<ModelIngestionProcessor>();

        var processor = new ModelIngestionProcessor(
            logger,
            modelRepository,
            layerRepository,
            atomRepository,
            relationRepository,
            orchestrator);

        var request = new ModelIngestionRequest { ModelPath = "C:/models/invalid.onnx" };
        var result = await processor.ProcessAsync(request);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Null(modelRepository.LastSavedModel);
        Assert.False(layerRepository.BulkInsertInvoked);
        Assert.Empty(atomRepository.AddedAtoms);
        Assert.Empty(relationRepository.AddedRelations);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error);
    }

    private static ModelIngestionOrchestrator CreateOrchestrator(Model modelToReturn, StubOnnxReader? readerOverride = null)
    {
        var discovery = new StubDiscoveryService(new ModelFormatInfo
        {
            Format = "ONNX",
            Confidence = 0.95
        });

        var reader = readerOverride ?? new StubOnnxReader();
        reader.ModelToReturn ??= modelToReturn;
        var services = new StubServiceProvider(new Dictionary<Type, object>
        {
            [typeof(IModelFormatReader<OnnxMetadata>)] = reader
        });

        return new ModelIngestionOrchestrator(
            discovery,
            services,
            TestLogger.Silent<ModelIngestionOrchestrator>());
    }

    private static Model CreateModel(string name)
    {
        return new Model
        {
            ModelName = name,
            ModelType = "LLM",
            ModelLayers = new List<ModelLayer>
            {
                new()
                {
                    LayerIdx = 0,
                    LayerName = "Embedding",
                    LayerType = "embedding",
                    ParameterCount = 128
                },
                new()
                {
                    LayerIdx = 1,
                    LayerName = "Attention",
                    LayerType = "attention",
                    ParameterCount = 256
                }
            }
        };
    }

    private sealed class StubModelRepository : IModelRepository
    {
        public Model? LastSavedModel { get; private set; }
        public Model ResultToReturn { get; set; } = new Model { ModelId = 1, ModelName = "Saved", ModelType = "LLM" };

        public Task<Model> AddAsync(Model model, CancellationToken cancellationToken = default)
        {
            LastSavedModel = model;
            return Task.FromResult(ResultToReturn);
        }

        public Task<Model?> GetByIdAsync(int modelId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Model?> GetByNameAsync(string modelName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<Model>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<Model>> GetByTypeAsync(string modelType, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(Model model, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(int modelId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsAsync(int modelId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> GetCountAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ModelLayer> AddLayerAsync(int modelId, ModelLayer layer, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateLayerWeightsAsync(int layerId, Microsoft.Data.SqlTypes.SqlVector<float> weights, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<ModelLayer>> GetLayersByModelIdAsync(int modelId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<Model>> GetModelsByCapabilityAsync(TaskType[] tasks, Modality requiredModalities = Modality.None, int minCount = 1, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Model>> GetByIdsAsync(IReadOnlyList<int> modelIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Model>> GetActiveModelsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubModelLayerRepository : IModelLayerRepository
    {
        public bool BulkInsertInvoked { get; private set; }
        public IReadOnlyList<ModelLayer>? LastLayers { get; private set; }

        public Task BulkInsertAsync(IEnumerable<ModelLayer> layers, CancellationToken cancellationToken = default)
        {
            BulkInsertInvoked = true;
            LastLayers = new List<ModelLayer>(layers);
            return Task.CompletedTask;
        }

        public Task<ModelLayer?> GetByIdAsync(long layerId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ModelLayer>> GetByModelAsync(int modelId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ModelLayer> AddAsync(ModelLayer layer, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(ModelLayer layer, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(long layerId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ModelLayer>> GetLayersByWeightRangeAsync(int modelId, double minValue, double maxValue, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ModelLayer>> GetLayersByImportanceAsync(int modelId, double minImportance, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public float[] ExtractWeightsFromGeometry(NetTopologySuite.Geometries.LineString geometry) => throw new NotSupportedException();
        public NetTopologySuite.Geometries.LineString CreateGeometryFromWeights(float[] weights, float[]? importanceScores = null, float[]? temporalMetadata = null) => throw new NotSupportedException();
    }

    private sealed class StubAtomRepository : IAtomRepository
    {
        private long _nextId = 1;

        public List<Atom> AddedAtoms { get; } = new();

        public Task<Atom> AddAsync(Atom atom, CancellationToken cancellationToken = default)
        {
            atom.AtomId = _nextId++;
            AddedAtoms.Add(atom);
            return Task.FromResult(atom);
        }

        public Task<Atom?> GetByContentHashAsync(byte[] contentHash, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Atom?> GetByIdAsync(long atomId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Atom>> GetByModalityAsync(string modality, int take = 100, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task IncrementReferenceCountAsync(long atomId, long delta = 1, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateMetadataAsync(long atomId, string? metadata, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateSpatialKeyAsync(long atomId, NetTopologySuite.Geometries.Point spatialKey, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubAtomRelationRepository : IAtomRelationRepository
    {
        private long _nextId = 1;

        public List<AtomRelation> AddedRelations { get; } = new();

        public Task<AtomRelation> AddAsync(AtomRelation relation, CancellationToken cancellationToken = default)
        {
            relation.AtomRelationId = _nextId++;
            AddedRelations.Add(relation);
            return Task.FromResult(relation);
        }

        public Task<AtomRelation?> GetByIdAsync(long relationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AtomRelation>> GetRelationsForAtomAsync(long atomId, int take = 256, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubDiscoveryService : IModelDiscoveryService
    {
        private readonly ModelFormatInfo _formatInfo;

        public StubDiscoveryService(ModelFormatInfo formatInfo)
        {
            _formatInfo = formatInfo;
        }

        public Task<ModelFormatInfo> DetectFormatAsync(string path, CancellationToken cancellationToken = default)
            => Task.FromResult(_formatInfo);

        public Task<IEnumerable<string>> GetModelFilesAsync(string modelPath, CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<string>>(Array.Empty<string>());

        public Task<bool> IsValidModelAsync(string path, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class StubServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services;

        public StubServiceProvider(Dictionary<Type, object> services)
        {
            _services = services;
        }

        public object? GetService(Type serviceType)
        {
            _services.TryGetValue(serviceType, out var service);
            return service;
        }
    }

    private sealed class StubOnnxReader : IModelFormatReader<OnnxMetadata>
    {
        public Model? ModelToReturn { get; set; }
        public bool ValidateResult { get; set; } = true;

        public string FormatName => "ONNX";
        public IEnumerable<string> SupportedExtensions => new[] { ".onnx" };

        public Task<Model> ReadAsync(string modelPath, CancellationToken cancellationToken = default)
        {
            if (ModelToReturn is null)
            {
                throw new InvalidOperationException("ModelToReturn not configured");
            }

            return Task.FromResult(ModelToReturn);
        }

        public Task<OnnxMetadata> GetMetadataAsync(string modelPath, CancellationToken cancellationToken = default)
            => Task.FromResult(new OnnxMetadata());

        public Task<bool> ValidateFormatAsync(string modelPath, CancellationToken cancellationToken = default)
            => Task.FromResult(ValidateResult);
    }
}


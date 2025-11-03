using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Entities;
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
        var processor = new ModelIngestionProcessor(
            TestLogger.Create<ModelIngestionProcessor>(),
            modelRepository,
            layerRepository,
            orchestrator);

        var request = new ModelIngestionRequest { ModelPath = "C:/models/model.onnx" };
        var result = await processor.ProcessAsync(request);

        Assert.True(result.Success);
        Assert.Equal(99, result.ModelId);
        Assert.Same(modelRepository.ResultToReturn, result.Model);
        Assert.Equal("Original", modelRepository.LastSavedModel?.ModelName);
        Assert.True(layerRepository.BulkInsertInvoked);
        Assert.All(layerRepository.LastLayers!, layer => Assert.Equal(99, layer.ModelId));
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
        var processor = new ModelIngestionProcessor(
            TestLogger.Create<ModelIngestionProcessor>(),
            modelRepository,
            layerRepository,
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
        var logger = TestLogger.Create<ModelIngestionProcessor>();

        var processor = new ModelIngestionProcessor(
            logger,
            modelRepository,
            layerRepository,
            orchestrator);

        var request = new ModelIngestionRequest { ModelPath = "C:/models/invalid.onnx" };
        var result = await processor.ProcessAsync(request);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Null(modelRepository.LastSavedModel);
        Assert.False(layerRepository.BulkInsertInvoked);
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
            Layers = new List<ModelLayer>
            {
                new()
                {
                    LayerId = 1,
                    LayerIdx = 0,
                    LayerName = "Embedding"
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Enums;
using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Services;
using Xunit;

namespace Hartonomous.UnitTests.Infrastructure;

public sealed class IngestionStatisticsServiceTests
{
    [Fact]
    public async Task GetStatsAsync_ComputesAggregates()
    {
        var models = new List<Model>
        {
            new()
            {
                ModelId = 1,
                ModelName = "Model-A",
                ModelType = "LLM",
                Architecture = "Transformer",
                ParameterCount = 100,
                Layers = new List<ModelLayer>
                {
                    new() { LayerId = 1 },
                    new() { LayerId = 2 }
                }
            },
            new()
            {
                ModelId = 2,
                ModelName = "Model-B",
                ModelType = "Vision",
                Architecture = null,
                ParameterCount = 50,
                Layers = new List<ModelLayer>
                {
                    new() { LayerId = 3 }
                }
            }
        };

        var repository = new StubModelRepository(models);
        var service = new IngestionStatisticsService(repository);

        var stats = await service.GetStatsAsync();

        Assert.Equal(2, stats.TotalModels);
        Assert.Equal(150, stats.TotalParameters);
        Assert.Equal(3, stats.TotalLayers);
        Assert.Equal(1, stats.ArchitectureBreakdown["Transformer"]);
        Assert.Equal(1, stats.ArchitectureBreakdown["Unknown"]);
    }

    [Fact]
    public async Task GetStatsAsync_WhenNoModels_ReturnsZeroes()
    {
        var repository = new StubModelRepository(new List<Model>());
        var service = new IngestionStatisticsService(repository);

        var stats = await service.GetStatsAsync();

        Assert.Equal(0, stats.TotalModels);
        Assert.Equal(0, stats.TotalParameters);
        Assert.Equal(0, stats.TotalLayers);
        Assert.Empty(stats.ArchitectureBreakdown);
    }

    private sealed class StubModelRepository : IModelRepository
    {
        private readonly IReadOnlyList<Model> _models;

        public StubModelRepository(IReadOnlyList<Model> models)
        {
            _models = models;
        }

        public Task<IEnumerable<Model>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_models.AsEnumerable());

        public Task<Model?> GetByIdAsync(int modelId, CancellationToken cancellationToken = default) => throw new System.NotSupportedException();
        public Task<Model?> GetByNameAsync(string modelName, CancellationToken cancellationToken = default) => throw new System.NotSupportedException();
        public Task<IEnumerable<Model>> GetByTypeAsync(string modelType, CancellationToken cancellationToken = default) => throw new System.NotSupportedException();
        public Task<Model> AddAsync(Model model, CancellationToken cancellationToken = default) => throw new System.NotSupportedException();
        public Task UpdateAsync(Model model, CancellationToken cancellationToken = default) => throw new System.NotSupportedException();
        public Task DeleteAsync(int modelId, CancellationToken cancellationToken = default) => throw new System.NotSupportedException();
        public Task<bool> ExistsAsync(int modelId, CancellationToken cancellationToken = default) => throw new System.NotSupportedException();
        public Task<int> GetCountAsync(CancellationToken cancellationToken = default) => throw new System.NotSupportedException();
        public Task<ModelLayer> AddLayerAsync(int modelId, ModelLayer layer, CancellationToken cancellationToken = default) => throw new System.NotSupportedException();
        public Task UpdateLayerWeightsAsync(int layerId, Microsoft.Data.SqlTypes.SqlVector<float> weights, CancellationToken cancellationToken = default) => throw new System.NotSupportedException();
        public Task<IEnumerable<ModelLayer>> GetLayersByModelIdAsync(int modelId, CancellationToken cancellationToken = default) => throw new System.NotSupportedException();
        public Task<IEnumerable<Model>> GetModelsByCapabilityAsync(TaskType[] tasks, Modality requiredModalities = Modality.None, int minCount = 1, CancellationToken cancellationToken = default) => throw new System.NotSupportedException();
        public Task<List<Model>> GetByIdsAsync(IReadOnlyList<int> modelIds, CancellationToken cancellationToken = default) => throw new System.NotSupportedException();
        public Task<List<Model>> GetActiveModelsAsync(CancellationToken cancellationToken = default) => throw new System.NotSupportedException();
    }
}

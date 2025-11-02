using Hartonomous.Core.Entities;
using Microsoft.Data.SqlTypes;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Repository abstraction for persisted AI model metadata and layers.
/// </summary>
public interface IModelRepository
{
    Task<Model?> GetByIdAsync(int modelId, CancellationToken cancellationToken = default);
    Task<Model?> GetByNameAsync(string modelName, CancellationToken cancellationToken = default);
    Task<IEnumerable<Model>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Model>> GetByTypeAsync(string modelType, CancellationToken cancellationToken = default);
    Task<Model> AddAsync(Model model, CancellationToken cancellationToken = default);
    Task UpdateAsync(Model model, CancellationToken cancellationToken = default);
    Task DeleteAsync(int modelId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int modelId, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    Task<ModelLayer> AddLayerAsync(int modelId, ModelLayer layer, CancellationToken cancellationToken = default);
    Task UpdateLayerWeightsAsync(int layerId, SqlVector<float> weights, CancellationToken cancellationToken = default);
    Task<IEnumerable<ModelLayer>> GetLayersByModelIdAsync(int modelId, CancellationToken cancellationToken = default);
}

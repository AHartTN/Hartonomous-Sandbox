using Hartonomous.Core.Entities;
using Hartonomous.Core.Enums;
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
    
    /// <summary>
    /// Query models by capability for ensemble orchestration.
    /// Returns models that support ANY of the specified tasks and ALL of the required modalities.
    /// </summary>
    /// <param name="tasks">Array of TaskType enums to filter by (OR logic - model supports any task)</param>
    /// <param name="requiredModalities">Modality flags that model must support (AND logic - model supports all modalities)</param>
    /// <param name="minCount">Minimum number of models to return (for ensemble requirements)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Models matching capability criteria, ordered by metadata quality</returns>
    Task<IEnumerable<Model>> GetModelsByCapabilityAsync(
        TaskType[] tasks,
        Modality requiredModalities = Modality.None,
        int minCount = 1,
        CancellationToken cancellationToken = default);

    Task<Model> AddAsync(Model model, CancellationToken cancellationToken = default);
    Task UpdateAsync(Model model, CancellationToken cancellationToken = default);
    Task DeleteAsync(int modelId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int modelId, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    Task<ModelLayer> AddLayerAsync(int modelId, ModelLayer layer, CancellationToken cancellationToken = default);
    Task UpdateLayerWeightsAsync(int layerId, SqlVector<float> weights, CancellationToken cancellationToken = default);
    Task<IEnumerable<ModelLayer>> GetLayersByModelIdAsync(int modelId, CancellationToken cancellationToken = default);
}

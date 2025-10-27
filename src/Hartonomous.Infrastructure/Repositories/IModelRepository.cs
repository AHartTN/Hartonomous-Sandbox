using Hartonomous.Core.Entities;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Model entity operations
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
}

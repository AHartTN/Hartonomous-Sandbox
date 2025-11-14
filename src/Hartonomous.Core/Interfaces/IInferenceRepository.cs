using Hartonomous.Data.Entities;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Repository abstraction for inference lifecycle operations and telemetry.
/// </summary>
public interface IInferenceRepository
{
    Task<InferenceRequest?> GetByIdAsync(long inferenceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<InferenceRequest>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default);
    Task<IEnumerable<InferenceRequest>> GetByTaskTypeAsync(string taskType, int count = 100, CancellationToken cancellationToken = default);
    Task<InferenceRequest> AddAsync(InferenceRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(InferenceRequest request, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    Task<double> GetAverageDurationAsync(string? taskType = null, CancellationToken cancellationToken = default);
}

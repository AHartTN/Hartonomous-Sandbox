using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Interface for model ingestion service.
/// </summary>
public interface IModelIngestionService : IService
{
    Task<int> IngestAsync(string modelPath, string? modelName = null, CancellationToken cancellationToken = default);
    Task<int[]> IngestDirectoryAsync(string directoryPath, string searchPattern = "*", CancellationToken cancellationToken = default);
    Task<IngestionStats> GetStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics for model ingestion operations.
/// </summary>
public class IngestionStats
{
    public int TotalModels { get; set; }
    public long TotalParameters { get; set; }
    public long TotalLayers { get; set; }
    public Dictionary<string, int> ArchitectureBreakdown { get; set; } = new();
}

/// <summary>
/// Request object for model ingestion.
/// </summary>
public class ModelIngestionRequest
{
    public string ModelPath { get; set; } = string.Empty;
    public string? CustomName { get; set; }
}

/// <summary>
/// Result of model ingestion operation.
/// </summary>
public class ModelIngestionResult
{
    public bool Success { get; set; }
    public int ModelId { get; set; }
    public Entities.Model? Model { get; set; }
    public string? ErrorMessage { get; set; }
}
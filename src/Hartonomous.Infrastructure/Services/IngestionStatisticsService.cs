using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Aggregates ingestion statistics by querying the model repository.
/// </summary>
public class IngestionStatisticsService : IIngestionStatisticsService
{
    /// <summary>
    /// Repository used to enumerate models and their metadata.
    /// </summary>
    private readonly IModelRepository _modelRepository;

    /// <summary>
    /// Creates a service that composes ingestion statistics from stored models.
    /// </summary>
    /// <param name="modelRepository">Repository providing model metadata.</param>
    public IngestionStatisticsService(IModelRepository modelRepository)
    {
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
    }

    /// <summary>
    /// Retrieves aggregate statistics for all ingested models.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>Model ingestion statistics with counts and architecture breakdown.</returns>
    public async Task<IngestionStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var models = await _modelRepository.GetAllAsync(cancellationToken);

        var stats = new IngestionStats
        {
            TotalModels = models.Count(),
            TotalParameters = models.Sum(m => m.ParameterCount ?? 0),
            TotalLayers = models.Sum(m => m.Layers?.Count ?? 0),
            ArchitectureBreakdown = models
                .GroupBy(m => m.Architecture ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return stats;
    }
}

using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Hartonomous.Infrastructure.Services;

public class IngestionStatisticsService : IIngestionStatisticsService
{
    private readonly IModelRepository _modelRepository;

    public IngestionStatisticsService(IModelRepository modelRepository)
    {
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
    }

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

using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Abstracts;
using Hartonomous.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Service for providing ingestion statistics.
/// Separated from ModelIngestionService for better separation of concerns.
/// </summary>
public class IngestionStatisticsService : BaseService, IIngestionStatisticsService
{
    private readonly IModelRepository _modelRepository;

    public IngestionStatisticsService(
        ILogger<IngestionStatisticsService> logger,
        IModelRepository modelRepository)
        : base(logger)
    {
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
    }

    public override string ServiceName => "IngestionStatisticsService";

    /// <summary>
    /// Get comprehensive ingestion statistics.
    /// </summary>
    public async Task<IngestionStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var models = await _modelRepository.GetAllAsync(cancellationToken);

        long totalParams = 0;
        long totalLayers = 0;
        var architectures = new Dictionary<string, int>();

        foreach (var model in models)
        {
            totalParams += model.ParameterCount ?? 0;

            var arch = model.Architecture ?? "Unknown";
            architectures[arch] = architectures.GetValueOrDefault(arch, 0) + 1;

            // Count layers for this model
            var layers = await _modelRepository.GetLayersByModelIdAsync(model.ModelId, cancellationToken);
            totalLayers += layers.Count();
        }

        return new IngestionStats
        {
            TotalModels = models.Count(),
            TotalParameters = totalParams,
            TotalLayers = totalLayers,
            ArchitectureBreakdown = architectures
        };
    }
}

/// <summary>
/// Interface for ingestion statistics service.
/// </summary>
public interface IIngestionStatisticsService : IService
{
    Task<IngestionStats> GetStatsAsync(CancellationToken cancellationToken = default);
}
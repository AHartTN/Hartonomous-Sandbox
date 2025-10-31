namespace Hartonomous.Core.Interfaces;

public interface IIngestionStatisticsService
{
    Task<IngestionStats> GetStatsAsync(CancellationToken cancellationToken = default);
}

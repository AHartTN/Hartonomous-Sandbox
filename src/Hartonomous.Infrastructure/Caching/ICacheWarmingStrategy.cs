using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Caching;

/// <summary>
/// Strategy for warming a specific cache type.
/// Enables polymorphic cache warming with different implementations per cache category.
/// </summary>
public interface ICacheWarmingStrategy
{
    /// <summary>
    /// Gets the cache type identifier this strategy handles.
    /// </summary>
    string CacheType { get; }

    /// <summary>
    /// Warms the cache by loading frequently accessed data.
    /// </summary>
    /// <param name="tenantId">Tenant to warm cache for (null = all tenants).</param>
    /// <param name="maxItems">Maximum number of items to warm.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of items warmed.</returns>
    Task<int> WarmAsync(int? tenantId, int maxItems, CancellationToken cancellationToken);
}

/// <summary>
/// Base class for cache warming strategies with common validation and logging.
/// Reduces duplication across cache warming implementations.
/// </summary>
public abstract class CacheWarmingStrategyBase : ICacheWarmingStrategy
{
    /// <inheritdoc />
    public abstract string CacheType { get; }

    /// <inheritdoc />
    public async Task<int> WarmAsync(int? tenantId, int maxItems, CancellationToken cancellationToken)
    {
        if (maxItems <= 0)
        {
            return 0;
        }

        try
        {
            LogWarmingStart(tenantId, maxItems);

            var warmedCount = await ExecuteWarmingAsync(tenantId, maxItems, cancellationToken);

            LogWarmingComplete(warmedCount);

            return warmedCount;
        }
        catch (Exception ex)
        {
            LogWarmingError(ex);
            throw;
        }
    }

    /// <summary>
    /// Executes the cache-specific warming logic.
    /// </summary>
    protected abstract Task<int> ExecuteWarmingAsync(int? tenantId, int maxItems, CancellationToken cancellationToken);

    /// <summary>
    /// Logs the start of cache warming.
    /// </summary>
    protected abstract void LogWarmingStart(int? tenantId, int maxItems);

    /// <summary>
    /// Logs the completion of cache warming.
    /// </summary>
    protected abstract void LogWarmingComplete(int warmedCount);

    /// <summary>
    /// Logs errors during cache warming.
    /// </summary>
    protected abstract void LogWarmingError(Exception ex);
}

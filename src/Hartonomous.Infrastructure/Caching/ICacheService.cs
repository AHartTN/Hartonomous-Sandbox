namespace Hartonomous.Infrastructure.Caching;

/// <summary>
/// Distributed cache service for storing and retrieving cached data.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value by key.
    /// </summary>
    /// <typeparam name="T">Type of cached value.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cached value or null if not found/expired.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Sets a cached value with optional expiration.
    /// </summary>
    /// <typeparam name="T">Type of value to cache.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Value to cache.</param>
    /// <param name="expiration">Expiration time (null = no expiration).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Removes a cached value by key.
    /// </summary>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in cache.
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or sets a cached value using a factory function.
    /// </summary>
    /// <typeparam name="T">Type of cached value.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Factory function to create value if not cached.</param>
    /// <param name="expiration">Expiration time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Removes all keys matching a pattern.
    /// </summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple cached values by keys.
    /// </summary>
    Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Sets multiple cached values.
    /// </summary>
    Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
}

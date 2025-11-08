using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Caching;

/// <summary>
/// Distributed cache implementation using IDistributedCache abstraction.
/// Supports Redis, SQL Server, NCache, Cosmos DB, and in-memory implementations
/// without code changes - just swap the registration in Program.cs
/// </summary>
public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public DistributedCacheService(
        IDistributedCache cache,
        ILogger<DistributedCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var bytes = await _cache.GetAsync(key, cancellationToken);
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(bytes, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache key {Key}: {Message}", key, ex.Message);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                // Default 24-hour expiration
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
            }

            await _cache.SetAsync(key, bytes, options, cancellationToken);
            _logger.LogDebug("Cached key {Key} with expiration {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache key {Key}: {Message}", key, ex.Message);
        }
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Removed cache key {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key {Key}: {Message}", key, ex.Message);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _cache.GetAsync(key, cancellationToken);
            return value != null && value.Length > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache key {Key}: {Message}", key, ex.Message);
            return false;
        }
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for key {Key}", key);
            return cached;
        }

        _logger.LogDebug("Cache miss for key {Key}, executing factory", key);
        var value = await factory();
        await SetAsync(key, value, expiration, cancellationToken);
        
        return value;
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // IDistributedCache doesn't support pattern-based operations
        // This would require provider-specific implementation or separate key tracking
        _logger.LogWarning("Pattern-based cache clearing not supported by IDistributedCache. Prefix: {Prefix}", prefix);
        await Task.CompletedTask;
    }

    public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        var result = new Dictionary<string, T?>();

        try
        {
            // IDistributedCache doesn't have batch operations, execute in parallel
            var tasks = keys.Select(async key =>
            {
                var value = await GetAsync<T>(key, cancellationToken);
                return (key, value);
            });

            var results = await Task.WhenAll(tasks);
            foreach (var (key, value) in results)
            {
                result[key] = value;
            }

            _logger.LogDebug("Retrieved {Count} keys, {HitCount} hits", 
                keys.Count(), result.Count(r => r.Value != null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting multiple cache keys: {Message}", ex.Message);
        }

        return result;
    }

    public async Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            // Execute in parallel since IDistributedCache doesn't have batch operations
            var tasks = items.Select(item => SetAsync(item.Key, item.Value, expiration, cancellationToken));
            await Task.WhenAll(tasks);

            _logger.LogDebug("Cached {Count} keys with expiration {Expiration}", items.Count, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting multiple cache keys: {Message}", ex.Message);
        }
    }
}

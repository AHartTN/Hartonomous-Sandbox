using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;

namespace Hartonomous.Infrastructure.HealthChecks;

/// <summary>
/// Health check for distributed cache connectivity and functionality.
/// </summary>
public class DistributedCacheHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;
    private const string HealthCheckKey = "__healthcheck__";

    public DistributedCacheHealthCheck(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Attempt write operation
            var testValue = $"healthcheck_{DateTime.UtcNow.Ticks}";
            var testBytes = Encoding.UTF8.GetBytes(testValue);

            await _cache.SetAsync(
                HealthCheckKey,
                testBytes,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
                },
                cancellationToken);

            // Attempt read operation
            var retrievedBytes = await _cache.GetAsync(HealthCheckKey, cancellationToken);

            if (retrievedBytes == null)
            {
                return HealthCheckResult.Degraded("Cache write succeeded but read failed");
            }

            var retrievedValue = Encoding.UTF8.GetString(retrievedBytes);

            if (retrievedValue != testValue)
            {
                return HealthCheckResult.Degraded("Cache read/write mismatch");
            }

            // Cleanup
            await _cache.RemoveAsync(HealthCheckKey, cancellationToken);

            var cacheType = _cache.GetType().Name;
            var data = new Dictionary<string, object>
            {
                ["CacheType"] = cacheType,
                ["OperationsSucceeded"] = "write, read, remove"
            };

            return HealthCheckResult.Healthy($"Distributed cache ({cacheType}) is functional", data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Distributed cache check failed", ex);
        }
    }
}

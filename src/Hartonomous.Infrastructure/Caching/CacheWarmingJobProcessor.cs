using Microsoft.Extensions.Logging;
using Hartonomous.Infrastructure.Caching;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Hartonomous.Infrastructure.Caching;

/// <summary>
/// Payload for cache warming jobs.
/// </summary>
public class CacheWarmingPayload
{
    /// <summary>
    /// Tenant ID to warm caches for (null = all tenants).
    /// </summary>
    public int? TenantId { get; set; }

    /// <summary>
    /// Cache types to warm (e.g., "Models", "Embeddings", "SearchResults").
    /// </summary>
    public required List<string> CacheTypes { get; set; }

    /// <summary>
    /// Maximum number of items to warm per cache type.
    /// </summary>
    public int MaxItemsPerType { get; set; } = 100;
}

/// <summary>
/// Result from cache warming job.
/// </summary>
public class CacheWarmingResult
{
    public Dictionary<string, int> WarmedCounts { get; set; } = new();
    public int TotalItemsWarmed { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Processes cache warming jobs to pre-populate frequently accessed data.
/// Proactively loads hot data into distributed cache during low-traffic periods.
/// </summary>
public class CacheWarmingJobProcessor : Infrastructure.Jobs.IJobProcessor<CacheWarmingPayload>
{
    private readonly ICacheService _cache;
    private readonly HartonomousDbContext _dbContext;
    private readonly ILogger<CacheWarmingJobProcessor> _logger;

    public string JobType => "CacheWarming";

    public CacheWarmingJobProcessor(
        ICacheService cache,
        HartonomousDbContext dbContext,
        ILogger<CacheWarmingJobProcessor> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<object?> ProcessAsync(
        CacheWarmingPayload payload,
        Infrastructure.Jobs.JobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var result = new CacheWarmingResult();

        _logger.LogInformation("Starting cache warming for types: {CacheTypes}", 
            string.Join(", ", payload.CacheTypes));

        foreach (var cacheType in payload.CacheTypes)
        {
            var warmedCount = await WarmCacheTypeAsync(cacheType, payload, cancellationToken);
            result.WarmedCounts[cacheType] = warmedCount;
            result.TotalItemsWarmed += warmedCount;

            _logger.LogInformation("Warmed {Count} items for cache type '{CacheType}'",
                warmedCount, cacheType);
        }

        result.Duration = DateTime.UtcNow - startTime;

        _logger.LogInformation("Cache warming completed: {TotalItems} items warmed in {DurationMs}ms",
            result.TotalItemsWarmed, result.Duration.TotalMilliseconds);

        return result;
    }

    private async Task<int> WarmCacheTypeAsync(
        string cacheType,
        CacheWarmingPayload payload,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        int warmedCount = 0;

        try
        {
            switch (cacheType.ToLowerInvariant())
            {
                case "models":
                    warmedCount = await WarmModelsCache(payload, cancellationToken);
                    break;

                case "tenants":
                    warmedCount = await WarmTenantsCache(payload, cancellationToken);
                    break;

                case "frequentoperations":
                    warmedCount = await WarmFrequentOperationsCache(payload, cancellationToken);
                    break;

                case "embeddings":
                    warmedCount = await WarmEmbeddingsCache(payload, cancellationToken);
                    break;

                default:
                    _logger.LogWarning("Unknown cache type '{CacheType}' - skipping", cacheType);
                    break;
            }

            sw.Stop();
            _logger.LogDebug("Warmed {Count} items for cache type '{CacheType}' in {ElapsedMs}ms",
                warmedCount, cacheType, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to warm cache type '{CacheType}'", cacheType);
        }

        return warmedCount;
    }

    /// <summary>
    /// Pre-loads frequently accessed model metadata into cache.
    /// Queries BillingUsageLedger for models with highest usage in last 7 days.
    /// </summary>
    private async Task<int> WarmModelsCache(CacheWarmingPayload payload, CancellationToken cancellationToken)
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        
        // Query most accessed models from billing ledger
        var topModels = await _dbContext.Set<Core.Entities.BillingUsageLedger>()
            .Where(b => b.TimestampUtc >= sevenDaysAgo 
                && b.Operation.Contains("Model")
                && (payload.TenantId == null || b.TenantId == payload.TenantId.ToString()))
            .GroupBy(b => b.MetadataJson) // MetadataJson contains model ID
            .Select(g => new { ModelMetadata = g.Key, UsageCount = g.Count() })
            .OrderByDescending(x => x.UsageCount)
            .Take(payload.MaxItemsPerType)
            .ToListAsync(cancellationToken);

        int cached = 0;
        foreach (var model in topModels)
        {
            if (!string.IsNullOrEmpty(model.ModelMetadata))
            {
                var cacheKey = $"model:metadata:{model.ModelMetadata.GetHashCode()}";
                await _cache.SetAsync(cacheKey, model.ModelMetadata, TimeSpan.FromHours(4), cancellationToken);
                cached++;
            }
        }

        return cached;
    }

    /// <summary>
    /// Pre-loads tenant configuration and quotas into cache.
    /// </summary>
    private async Task<int> WarmTenantsCache(CacheWarmingPayload payload, CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<Core.Entities.BillingUsageLedger>()
            .Where(b => payload.TenantId == null || b.TenantId == payload.TenantId.ToString())
            .GroupBy(b => b.TenantId)
            .Select(g => new { TenantId = g.Key, LastActivity = g.Max(x => x.TimestampUtc) })
            .OrderByDescending(x => x.LastActivity)
            .Take(payload.MaxItemsPerType);

        var activeTenants = await query.ToListAsync(cancellationToken);

        int cached = 0;
        foreach (var tenant in activeTenants)
        {
            var cacheKey = $"tenant:config:{tenant.TenantId}";
            await _cache.SetAsync(cacheKey, tenant, TimeSpan.FromHours(1), cancellationToken);
            cached++;
        }

        return cached;
    }

    /// <summary>
    /// Pre-caches frequently performed operations for fast lookup.
    /// </summary>
    private async Task<int> WarmFrequentOperationsCache(CacheWarmingPayload payload, CancellationToken cancellationToken)
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        var topOperations = await _dbContext.Set<Core.Entities.BillingUsageLedger>()
            .Where(b => b.TimestampUtc >= sevenDaysAgo
                && (payload.TenantId == null || b.TenantId == payload.TenantId.ToString()))
            .GroupBy(b => new { b.Operation, b.TenantId })
            .Select(g => new { 
                Operation = g.Key.Operation, 
                TenantId = g.Key.TenantId,
                CallCount = g.Count(),
                AvgCost = g.Average(x => x.TotalCost)
            })
            .OrderByDescending(x => x.CallCount)
            .Take(payload.MaxItemsPerType)
            .ToListAsync(cancellationToken);

        int cached = 0;
        foreach (var op in topOperations)
        {
            var cacheKey = $"operation:stats:{op.TenantId}:{op.Operation}";
            await _cache.SetAsync(cacheKey, op, TimeSpan.FromMinutes(30), cancellationToken);
            cached++;
        }

        return cached;
    }

    /// <summary>
    /// Pre-loads frequently accessed embeddings into cache.
    /// </summary>
    private async Task<int> WarmEmbeddingsCache(CacheWarmingPayload payload, CancellationToken cancellationToken)
    {
        // Query top N most recently accessed embeddings
        var recentEmbeddings = await _dbContext.Set<Core.Entities.AtomEmbedding>()
            .Where(e => payload.TenantId == null || e.TenantId == payload.TenantId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(payload.MaxItemsPerType)
            .Select(e => new { e.EmbeddingId, e.TenantId, e.EmbeddingVector })
            .ToListAsync(cancellationToken);

        int cached = 0;
        foreach (var embedding in recentEmbeddings)
        {
            var cacheKey = $"embedding:{embedding.TenantId}:{embedding.EmbeddingId}";
            await _cache.SetAsync(cacheKey, embedding.EmbeddingVector, TimeSpan.FromHours(2), cancellationToken);
            cached++;
        }

        return cached;
    }
}

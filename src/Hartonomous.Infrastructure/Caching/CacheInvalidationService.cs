using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Caching;

/// <summary>
/// Service for managing cache invalidation strategies.
/// </summary>
public class CacheInvalidationService
{
    private readonly ICacheService _cache;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(
        ICacheService cache,
        ILogger<CacheInvalidationService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Invalidates all embedding caches for an atom.
    /// </summary>
    public async Task InvalidateAtomEmbeddingsAsync(long atomId, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(CacheKeys.EmbeddingsByAtom(atomId), cancellationToken);
        _logger.LogInformation("Invalidated embedding cache for atom {AtomId}", atomId);
    }

    /// <summary>
    /// Invalidates all model caches.
    /// </summary>
    public async Task InvalidateModelAsync(int modelId, CancellationToken cancellationToken = default)
    {
        await Task.WhenAll(
            _cache.RemoveAsync(CacheKeys.ModelById(modelId), cancellationToken),
            _cache.RemoveAsync(CacheKeys.ModelLayers(modelId), cancellationToken),
            _cache.RemoveByPrefixAsync($"model:{modelId}:", cancellationToken)
        );
        
        _logger.LogInformation("Invalidated all caches for model {ModelId}", modelId);
    }

    /// <summary>
    /// Invalidates inference result cache when model is updated.
    /// </summary>
    public async Task InvalidateInferenceResultsAsync(int modelId, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(CacheKeys.InferencesByModel(modelId), cancellationToken);
        _logger.LogInformation("Invalidated inference results for model {ModelId}", modelId);
    }

    /// <summary>
    /// Invalidates all search result caches.
    /// </summary>
    public async Task InvalidateSearchResultsAsync(CancellationToken cancellationToken = default)
    {
        await _cache.RemoveByPrefixAsync(CacheKeys.SearchPrefix, cancellationToken);
        _logger.LogInformation("Invalidated all search result caches");
    }

    /// <summary>
    /// Invalidates all caches for a specific tenant.
    /// </summary>
    public async Task InvalidateTenantCachesAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        await Task.WhenAll(
            _cache.RemoveAsync(CacheKeys.AtomsByTenant(tenantId), cancellationToken),
            _cache.RemoveAsync(CacheKeys.EmbeddingsByTenant(tenantId), cancellationToken),
            _cache.RemoveAsync(CacheKeys.ModelsByTenant(tenantId), cancellationToken),
            _cache.RemoveByPrefixAsync($"billing:tenant:{tenantId}", cancellationToken),
            _cache.RemoveByPrefixAsync($"analytics:tenant:{tenantId}", cancellationToken)
        );
        
        _logger.LogInformation("Invalidated all caches for tenant {TenantId}", tenantId);
    }

    /// <summary>
    /// Invalidates analytics caches for a specific date.
    /// </summary>
    public async Task InvalidateAnalyticsCacheAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(CacheKeys.DailyUsage(date), cancellationToken);
        _logger.LogInformation("Invalidated analytics cache for date {Date}", date);
    }

    /// <summary>
    /// Invalidates graph traversal caches for an atom.
    /// </summary>
    public async Task InvalidateGraphCachesAsync(long atomId, CancellationToken cancellationToken = default)
    {
        await Task.WhenAll(
            _cache.RemoveAsync(CacheKeys.AtomRelationships(atomId), cancellationToken),
            _cache.RemoveByPrefixAsync($"graph:traversal:{atomId}", cancellationToken),
            _cache.RemoveByPrefixAsync($"graph:path:{atomId}", cancellationToken)
        );
        
        _logger.LogInformation("Invalidated graph caches for atom {AtomId}", atomId);
    }

    /// <summary>
    /// Warms up frequently accessed caches.
    /// </summary>
    public async Task WarmCachesAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting cache warming for tenant {TenantId}", tenantId);
        
        // This would be called by a background job
        // Implementation would pre-load frequently accessed data
        
        await Task.CompletedTask;
    }
}

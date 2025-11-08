namespace Hartonomous.Infrastructure.Caching;

/// <summary>
/// Centralized cache key definitions with consistent naming.
/// </summary>
public static class CacheKeys
{
    // Embedding cache keys
    public static string EmbeddingById(long embeddingId) => $"embedding:{embeddingId}";
    public static string EmbeddingsByAtom(long atomId) => $"embeddings:atom:{atomId}";
    public static string EmbeddingsByTenant(int tenantId) => $"embeddings:tenant:{tenantId}";

    // Model cache keys
    public static string ModelById(int modelId) => $"model:{modelId}";
    public static string ModelsByTenant(int tenantId) => $"models:tenant:{tenantId}";
    public static string ModelLayers(int modelId) => $"model:{modelId}:layers";
    public static string ModelWeights(int modelId, int layerId) => $"model:{modelId}:layer:{layerId}:weights";

    // Inference cache keys
    public static string InferenceById(long inferenceId) => $"inference:{inferenceId}";
    public static string InferencesByModel(int modelId) => $"inferences:model:{modelId}";
    public static string InferenceResult(string inputHash) => $"inference:result:{inputHash}";

    // Atom cache keys
    public static string AtomById(long atomId) => $"atom:{atomId}";
    public static string AtomsByTenant(int tenantId) => $"atoms:tenant:{tenantId}";
    public static string AtomRelationships(long atomId) => $"atom:{atomId}:relationships";

    // Search result cache keys
    public static string SearchResults(string queryHash) => $"search:results:{queryHash}";
    public static string SpatialSearchResults(string queryHash) => $"search:spatial:{queryHash}";
    public static string TemporalSearchResults(string queryHash) => $"search:temporal:{queryHash}";

    // Analytics cache keys
    public static string TenantMetrics(int tenantId, DateTime date) => 
        $"analytics:tenant:{tenantId}:metrics:{date:yyyyMMdd}";
    public static string ModelPerformance(int modelId, DateTime date) => 
        $"analytics:model:{modelId}:performance:{date:yyyyMMdd}";
    public static string DailyUsage(DateTime date) => $"analytics:usage:{date:yyyyMMdd}";

    // Billing cache keys
    public static string TenantQuota(int tenantId, string usageType) => 
        $"billing:tenant:{tenantId}:quota:{usageType}";
    public static string UsageRecords(int tenantId, DateTime date) => 
        $"billing:tenant:{tenantId}:usage:{date:yyyyMMdd}";

    // Graph cache keys
    public static string GraphTraversal(long startAtomId, int depth) => 
        $"graph:traversal:{startAtomId}:depth:{depth}";
    public static string GraphShortestPath(long fromId, long toId) => 
        $"graph:path:{fromId}:{toId}";

    // Prefix patterns for invalidation
    public const string EmbeddingPrefix = "embedding";
    public const string ModelPrefix = "model";
    public const string InferencePrefix = "inference";
    public const string AtomPrefix = "atom";
    public const string SearchPrefix = "search";
    public const string AnalyticsPrefix = "analytics";
    public const string BillingPrefix = "billing";
    public const string GraphPrefix = "graph";

    // Tenant-specific invalidation
    public static string TenantPrefix(int tenantId) => $"tenant:{tenantId}";
}

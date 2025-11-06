using System.Security.Claims;
using System.Threading.RateLimiting;

namespace Hartonomous.Api.RateLimiting;

/// <summary>
/// Tenant-aware rate limiting policy that reads limits from configuration or database.
/// Allows different rate limits based on tenant tier (Free, Basic, Premium, Enterprise).
/// </summary>
public class TenantRateLimitPolicy
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantRateLimitPolicy> _logger;

    // Default rate limits by tier (requests per minute)
    private static readonly Dictionary<string, int> DefaultTierLimits = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Free", 10 },
        { "Basic", 100 },
        { "Premium", 500 },
        { "Enterprise", 2000 },
        { "Admin", int.MaxValue } // No limits for admins
    };

    public TenantRateLimitPolicy(IConfiguration configuration, ILogger<TenantRateLimitPolicy> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a rate limiter partition based on tenant ID and tier.
    /// </summary>
    public RateLimitPartition<string> GetPartition(HttpContext context)
    {
        // Extract tenant ID from claims
        var tenantIdClaim = context.User.FindFirst("tenant_id") 
            ?? context.User.FindFirst("extension_TenantId")
            ?? context.User.FindFirst(ClaimTypes.GroupSid);

        var tenantId = tenantIdClaim?.Value ?? "anonymous";

        // Extract tenant tier from claims (or default to Free)
        var tierClaim = context.User.FindFirst("tenant_tier") 
            ?? context.User.FindFirst("extension_TenantTier");
        var tier = tierClaim?.Value ?? "Free";

        // Admin users get unlimited
        if (context.User.IsInRole("Admin"))
        {
            tier = "Admin";
        }

        // Get rate limit for this tier
        var permitLimit = GetRateLimitForTier(tier);

        // Create partition key (tenant ID + tier for isolation)
        var partitionKey = $"{tenantId}:{tier}";

        _logger.LogDebug("Rate limit for tenant {TenantId} (tier: {Tier}): {Limit} requests/min",
            tenantId, tier, permitLimit);

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = Math.Min(permitLimit / 10, 10) // 10% of limit, max 10
            });
    }

    /// <summary>
    /// Gets the rate limit for a specific tier from configuration or defaults.
    /// </summary>
    private int GetRateLimitForTier(string tier)
    {
        // Try configuration first (allows runtime overrides)
        var configKey = $"RateLimiting:TenantTiers:{tier}";
        var configValue = _configuration.GetValue<int?>(configKey);

        if (configValue.HasValue && configValue.Value > 0)
        {
            return configValue.Value;
        }

        // Fall back to defaults
        if (DefaultTierLimits.TryGetValue(tier, out var defaultLimit))
        {
            return defaultLimit;
        }

        // Unknown tier - use most restrictive (Free tier)
        _logger.LogWarning("Unknown tenant tier '{Tier}', defaulting to Free tier limits", tier);
        return DefaultTierLimits["Free"];
    }

    /// <summary>
    /// Creates a sliding window partition for more sophisticated rate limiting.
    /// Useful for inference endpoints or other resource-intensive operations.
    /// </summary>
    public RateLimitPartition<string> GetSlidingWindowPartition(HttpContext context)
    {
        var tenantIdClaim = context.User.FindFirst("tenant_id") 
            ?? context.User.FindFirst("extension_TenantId")
            ?? context.User.FindFirst(ClaimTypes.GroupSid);

        var tenantId = tenantIdClaim?.Value ?? "anonymous";

        var tierClaim = context.User.FindFirst("tenant_tier") 
            ?? context.User.FindFirst("extension_TenantTier");
        var tier = tierClaim?.Value ?? "Free";

        if (context.User.IsInRole("Admin"))
        {
            tier = "Admin";
        }

        var permitLimit = GetRateLimitForTier(tier);
        var partitionKey = $"{tenantId}:{tier}";

        return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ =>
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6, // 10-second segments
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = Math.Min(permitLimit / 10, 10)
            });
    }

    /// <summary>
    /// Creates a token bucket partition for inference/generation endpoints.
    /// Allows bursts while maintaining average rate over time.
    /// </summary>
    public RateLimitPartition<string> GetTokenBucketPartition(HttpContext context)
    {
        var tenantIdClaim = context.User.FindFirst("tenant_id") 
            ?? context.User.FindFirst("extension_TenantId")
            ?? context.User.FindFirst(ClaimTypes.GroupSid);

        var tenantId = tenantIdClaim?.Value ?? "anonymous";

        var tierClaim = context.User.FindFirst("tenant_tier") 
            ?? context.User.FindFirst("extension_TenantTier");
        var tier = tierClaim?.Value ?? "Free";

        if (context.User.IsInRole("Admin"))
        {
            tier = "Admin";
        }

        // Token bucket allows bursts - use half the tier limit as token limit
        var baseLimit = GetRateLimitForTier(tier);
        var tokenLimit = Math.Max(baseLimit / 2, 5); // At least 5 tokens
        var tokensPerPeriod = Math.Max(baseLimit / 12, 1); // Replenish ~5 seconds

        var partitionKey = $"{tenantId}:{tier}";

        return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ =>
            new TokenBucketRateLimiterOptions
            {
                TokenLimit = tokenLimit,
                TokensPerPeriod = tokensPerPeriod,
                ReplenishmentPeriod = TimeSpan.FromSeconds(5),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = Math.Min(tokenLimit / 5, 5),
                AutoReplenishment = true
            });
    }
}

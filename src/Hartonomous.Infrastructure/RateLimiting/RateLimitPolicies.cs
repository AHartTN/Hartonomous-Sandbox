using System.Threading.RateLimiting;

namespace Hartonomous.Infrastructure.RateLimiting;

/// <summary>
/// Rate limit policy names for different API tiers and operations.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>
    /// Standard rate limit for authenticated users (100 req/min).
    /// </summary>
    public const string Authenticated = "authenticated";

    /// <summary>
    /// Strict rate limit for anonymous users (10 req/min).
    /// </summary>
    public const string Anonymous = "anonymous";

    /// <summary>
    /// Premium tier with higher limits (1000 req/min).
    /// </summary>
    public const string Premium = "premium";

    /// <summary>
    /// Rate limit for inference operations (10 concurrent, 100/min).
    /// </summary>
    public const string Inference = "inference";

    /// <summary>
    /// Rate limit for generation operations (5 concurrent, 20/min).
    /// </summary>
    public const string Generation = "generation";

    /// <summary>
    /// Rate limit for embedding operations (50/min).
    /// </summary>
    public const string Embedding = "embedding";

    /// <summary>
    /// Rate limit for graph operations (30/min).
    /// </summary>
    public const string Graph = "graph";

    /// <summary>
    /// Global rate limit per IP address (200 req/min).
    /// </summary>
    public const string Global = "global";
}

/// <summary>
/// Configuration options for rate limiting policies.
/// </summary>
public class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Permit limit for authenticated users.
    /// </summary>
    public int AuthenticatedPermitLimit { get; set; } = 100;

    /// <summary>
    /// Window duration in seconds for authenticated users.
    /// </summary>
    public int AuthenticatedWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Permit limit for anonymous users.
    /// </summary>
    public int AnonymousPermitLimit { get; set; } = 10;

    /// <summary>
    /// Window duration in seconds for anonymous users.
    /// </summary>
    public int AnonymousWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Permit limit for premium users.
    /// </summary>
    public int PremiumPermitLimit { get; set; } = 1000;

    /// <summary>
    /// Window duration in seconds for premium users.
    /// </summary>
    public int PremiumWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Concurrent request limit for inference operations.
    /// </summary>
    public int InferenceConcurrentLimit { get; set; } = 10;

    /// <summary>
    /// Permit limit for inference operations.
    /// </summary>
    public int InferencePermitLimit { get; set; } = 100;

    /// <summary>
    /// Window duration in seconds for inference operations.
    /// </summary>
    public int InferenceWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Concurrent request limit for generation operations.
    /// </summary>
    public int GenerationConcurrentLimit { get; set; } = 5;

    /// <summary>
    /// Permit limit for generation operations.
    /// </summary>
    public int GenerationPermitLimit { get; set; } = 20;

    /// <summary>
    /// Window duration in seconds for generation operations.
    /// </summary>
    public int GenerationWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Permit limit for embedding operations.
    /// </summary>
    public int EmbeddingPermitLimit { get; set; } = 50;

    /// <summary>
    /// Window duration in seconds for embedding operations.
    /// </summary>
    public int EmbeddingWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Permit limit for graph operations.
    /// </summary>
    public int GraphPermitLimit { get; set; } = 30;

    /// <summary>
    /// Window duration in seconds for graph operations.
    /// </summary>
    public int GraphWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Global permit limit per IP address.
    /// </summary>
    public int GlobalPermitLimit { get; set; } = 200;

    /// <summary>
    /// Global window duration in seconds.
    /// </summary>
    public int GlobalWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Queue limit for rate limited requests (0 to disable queueing).
    /// </summary>
    public int QueueLimit { get; set; } = 10;

    /// <summary>
    /// Segments per window for sliding window algorithm.
    /// </summary>
    public int SegmentsPerWindow { get; set; } = 8;
}

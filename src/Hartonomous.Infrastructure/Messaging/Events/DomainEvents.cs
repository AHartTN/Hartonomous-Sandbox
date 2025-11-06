namespace Hartonomous.Infrastructure.Messaging.Events;

/// <summary>
/// Event published when an atom is ingested.
/// </summary>
public class AtomIngestedEvent : IntegrationEvent
{
    public required long AtomId { get; init; }
    public required string ContentType { get; init; }
    public long? EmbeddingId { get; init; }
}

/// <summary>
/// Event published when an embedding is generated.
/// </summary>
public class EmbeddingGeneratedEvent : IntegrationEvent
{
    public required long EmbeddingId { get; init; }
    public required long AtomId { get; init; }
    public required string SourceType { get; init; }
    public int VectorDimensions { get; init; } = 768;
}

/// <summary>
/// Event published when a model is ingested.
/// </summary>
public class ModelIngestedEvent : IntegrationEvent
{
    public required int ModelId { get; init; }
    public required string ModelName { get; init; }
    public required string Architecture { get; init; }
    public int LayerCount { get; init; }
    public long TotalParameters { get; init; }
}

/// <summary>
/// Event published when inference completes.
/// </summary>
public class InferenceCompletedEvent : IntegrationEvent
{
    public required long InferenceId { get; init; }
    public required int ModelId { get; init; }
    public required string Status { get; init; }
    public long DurationMs { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Event published when cache is invalidated.
/// </summary>
public class CacheInvalidatedEvent : IntegrationEvent
{
    public required string CacheType { get; init; }
    public List<string>? InvalidatedKeys { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Event published when a tenant quota is exceeded.
/// </summary>
public class QuotaExceededEvent : IntegrationEvent
{
    public required string UsageType { get; init; }
    public required long CurrentUsage { get; init; }
    public required long QuotaLimit { get; init; }
    public required string TenantTier { get; init; }
}

namespace Hartonomous.Infrastructure.Messaging.Events;

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

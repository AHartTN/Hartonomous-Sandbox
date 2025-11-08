namespace Hartonomous.Infrastructure.Messaging.Events;

/// <summary>
/// Event published when cache is invalidated.
/// </summary>
public class CacheInvalidatedEvent : IntegrationEvent
{
    public required string CacheType { get; init; }
    public List<string>? InvalidatedKeys { get; init; }
    public string? Reason { get; init; }
}

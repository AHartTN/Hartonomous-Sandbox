namespace Hartonomous.Infrastructure.Messaging.Events;

/// <summary>
/// Base class for integration events published to the event bus.
/// </summary>
public abstract class IntegrationEvent
{
    /// <summary>
    /// Unique event identifier.
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Tenant ID (for multi-tenant scenarios).
    /// </summary>
    public int? TenantId { get; init; }

    /// <summary>
    /// User ID who triggered the event.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Correlation ID for tracking related events.
    /// </summary>
    public string? CorrelationId { get; init; }
}

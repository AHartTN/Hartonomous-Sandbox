using Hartonomous.Infrastructure.Messaging.Events;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Messaging.Handlers;

/// <summary>
/// Handles atom ingestion events.
/// </summary>
public class AtomIngestedEventHandler
{
    private readonly ILogger<AtomIngestedEventHandler> _logger;
    private readonly IEventBus _eventBus;

    public AtomIngestedEventHandler(
        ILogger<AtomIngestedEventHandler> logger,
        IEventBus eventBus)
    {
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task HandleAsync(AtomIngestedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Atom ingested: AtomId={AtomId}, ContentType={ContentType}",
            @event.AtomId, @event.ContentType);

        // Publish OODA observation event
        if (@event.EmbeddingId.HasValue)
        {
            var observationEvent = new ObservationEvent
            {
                AtomId = @event.AtomId,
                SourceType = @event.ContentType,
                EmbeddingId = @event.EmbeddingId,
                TenantId = @event.TenantId,
                UserId = @event.UserId,
                CorrelationId = @event.CorrelationId
            };

            await _eventBus.PublishAsync(observationEvent, cancellationToken: cancellationToken);
        }
    }
}

/// <summary>
/// Handles cache invalidation events.
/// </summary>
public class CacheInvalidatedEventHandler
{
    private readonly ILogger<CacheInvalidatedEventHandler> _logger;

    public CacheInvalidatedEventHandler(
        ILogger<CacheInvalidatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(CacheInvalidatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cache invalidation: Type={CacheType}, Reason={Reason}",
            @event.CacheType, @event.Reason);

        await Task.CompletedTask;
    }
}

/// <summary>
/// Handles quota exceeded events.
/// </summary>
public class QuotaExceededEventHandler
{
    private readonly ILogger<QuotaExceededEventHandler> _logger;

    public QuotaExceededEventHandler(ILogger<QuotaExceededEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(QuotaExceededEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Quota exceeded: TenantId={TenantId}, UsageType={UsageType}, Current={Current}, Limit={Limit}, Tier={Tier}",
            @event.TenantId, @event.UsageType, @event.CurrentUsage, @event.QuotaLimit, @event.TenantTier);

        // Future: Send notification, update dashboard, trigger upgrade workflow
        await Task.CompletedTask;
    }
}

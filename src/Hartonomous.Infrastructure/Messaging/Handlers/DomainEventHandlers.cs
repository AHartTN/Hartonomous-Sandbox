using Hartonomous.Infrastructure.Caching;
using Hartonomous.Infrastructure.Messaging.Events;
using Microsoft.Extensions.Logging;
using Hartonomous.Data.Entities;

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
    private readonly CacheInvalidationService _cacheInvalidation;

    public CacheInvalidatedEventHandler(
        ILogger<CacheInvalidatedEventHandler> logger,
        CacheInvalidationService cacheInvalidation)
    {
        _logger = logger;
        _cacheInvalidation = cacheInvalidation;
    }

    public async Task HandleAsync(CacheInvalidatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cache invalidation: Type={CacheType}, Reason={Reason}",
            @event.CacheType, @event.Reason);

        // Perform cache invalidation based on type
        switch (@event.CacheType.ToLowerInvariant())
        {
            case "search":
                await _cacheInvalidation.InvalidateSearchResultsAsync(cancellationToken);
                break;
            
            case "analytics":
                if (@event.InvalidatedKeys != null && @event.InvalidatedKeys.Any())
                {
                    var date = DateTime.UtcNow.Date;
                    await _cacheInvalidation.InvalidateAnalyticsCacheAsync(date, cancellationToken);
                }
                break;

            default:
                _logger.LogWarning("Unknown cache type for invalidation: {CacheType}", @event.CacheType);
                break;
        }
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

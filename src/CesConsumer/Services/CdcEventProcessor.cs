using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Mappers;
using Hartonomous.Core.Models;
using Hartonomous.Core.Services;
using Microsoft.Extensions.Logging;

namespace CesConsumer.Services;

/// <summary>
/// Processes SQL Server Change Data Capture (CDC) events and publishes enriched events to messaging infrastructure.
/// Thin orchestrator that delegates to mapper, enricher, and publisher.
/// </summary>
public class CdcEventProcessor : BaseEventProcessor
{
    private readonly ICdcRepository _cdcRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IEventEnricher _enricher;
    private readonly ICdcCheckpointManager _checkpointManager;
    private readonly IEventMapperBidirectional<CdcChangeEvent, BaseEvent> _mapper;
    private string? _lastLsn;

    public CdcEventProcessor(
        ICdcRepository cdcRepository,
        IEventPublisher eventPublisher,
        IEventEnricher enricher,
        ILogger<CdcEventProcessor> logger,
        ICdcCheckpointManager checkpointManager,
        IEventMapperBidirectional<CdcChangeEvent, BaseEvent> mapper)
        : base(logger)
    {
        _cdcRepository = cdcRepository ?? throw new ArgumentNullException(nameof(cdcRepository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _enricher = enricher ?? throw new ArgumentNullException(nameof(enricher));
        _checkpointManager = checkpointManager ?? throw new ArgumentNullException(nameof(checkpointManager));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    protected override async Task OnStartingAsync(CancellationToken cancellationToken)
    {
        _lastLsn = await _checkpointManager.GetLastProcessedLsnAsync(cancellationToken);
        Logger.LogInformation("Starting from LSN: {LastLsn}", _lastLsn ?? "Beginning");
    }

    protected override async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var changeEvents = await _cdcRepository.GetChangeEventsSinceAsync(_lastLsn, cancellationToken);
        
        if (!changeEvents.Any())
        {
            return;
        }

        // Map CDC events to platform events
        var events = _mapper.MapMany(changeEvents).ToList();

        // Enrich all events
        await _enricher.EnrichBatchAsync(events, cancellationToken);

        // Publish in batch
        await _eventPublisher.PublishBatchAsync(events, cancellationToken);

        // Update checkpoint
        var maxLsn = changeEvents.Max(e => e.Lsn);
        if (maxLsn != null)
        {
            await _checkpointManager.UpdateLastProcessedLsnAsync(maxLsn, cancellationToken);
            _lastLsn = maxLsn;
            Logger.LogInformation("Processed {Count} change events, new LSN: {MaxLsn}", 
                changeEvents.Count(), maxLsn);
        }
    }
}

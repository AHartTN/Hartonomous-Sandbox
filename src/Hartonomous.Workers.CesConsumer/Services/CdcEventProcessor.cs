using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Mappers;
using Hartonomous.Core.Models;
using Hartonomous.Core.Performance;
using Hartonomous.Core.Services;
using Microsoft.Extensions.Logging;

namespace CesConsumer.Services;

/// <summary>
/// Processes SQL Server Change Data Capture (CDC) events and publishes enriched events to the message broker.
/// OPTIMIZED: Zero-allocation hot path, pre-sized collections, removed LINQ.
/// </summary>
public class CdcEventProcessor : BaseEventProcessor
{
    private readonly ICdcRepository _cdcRepository;
    private readonly IMessageBroker _messageBroker;
    private readonly IEventEnricher _enricher;
    private readonly ICdcCheckpointManager _checkpointManager;
    private readonly IEventMapperBidirectional<CdcChangeEvent, BaseEvent> _mapper;
    private string? _lastLsn;

    public CdcEventProcessor(
        ICdcRepository cdcRepository,
        IMessageBroker messageBroker,
        IEventEnricher enricher,
        ILogger<CdcEventProcessor> logger,
        ICdcCheckpointManager checkpointManager,
        IEventMapperBidirectional<CdcChangeEvent, BaseEvent> mapper)
        : base(logger)
    {
        _cdcRepository = cdcRepository ?? throw new ArgumentNullException(nameof(cdcRepository));
        _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
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

        // Early exit if no changes (OPTIMIZED: avoid allocations)
        if (changeEvents.Count == 0)
        {
            return;
        }

        // Pre-allocate with exact capacity (OPTIMIZED: avoid resizing)
        var events = new List<BaseEvent>(changeEvents.Count);

        // Map CDC events to platform events (OPTIMIZED: removed LINQ)
        foreach (var changeEvent in changeEvents)
        {
            var mapped = _mapper.Map(changeEvent);
            if (mapped != null)
            {
                events.Add(mapped);
            }
        }

        // Skip enrichment/publishing if all mappings failed
        if (events.Count == 0)
        {
            Logger.LogWarning("All {Count} change events failed to map", changeEvents.Count);
            return;
        }

        // Enrich all events
        await _enricher.EnrichBatchAsync(events, cancellationToken);

        // Publish in batch
        await _messageBroker.PublishBatchAsync(events, cancellationToken);

        // Find max LSN (OPTIMIZED: manual iteration instead of LINQ Max)
        string? maxLsn = null;
        foreach (var changeEvent in changeEvents)
        {
            if (changeEvent.Lsn != null)
            {
                if (maxLsn == null || StringComparer.Ordinal.Compare(changeEvent.Lsn, maxLsn) > 0)
                {
                    maxLsn = changeEvent.Lsn;
                }
            }
        }

        // Update checkpoint
        if (maxLsn != null)
        {
            await _checkpointManager.UpdateLastProcessedLsnAsync(maxLsn, cancellationToken);
            _lastLsn = maxLsn;
            Logger.LogInformation("Processed {Count} change events, new LSN: {MaxLsn}",
                changeEvents.Count, maxLsn);
        }
    }
}

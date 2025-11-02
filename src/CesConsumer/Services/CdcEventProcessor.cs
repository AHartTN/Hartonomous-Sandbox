using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Models;
using Hartonomous.Core.Services;
using Microsoft.Extensions.Logging;

namespace CesConsumer.Services;

/// <summary>
/// Processes SQL Server Change Data Capture (CDC) events and publishes enriched events to messaging infrastructure.
/// Orchestrates retrieval, enrichment, publishing, and checkpointing of database change events.
/// </summary>
public class CdcEventProcessor : BaseEventProcessor
{
    private readonly ICdcRepository _cdcRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IEventEnricher _enricher;
    private readonly ICdcCheckpointManager _checkpointManager;
    private string? _lastLsn;

    public CdcEventProcessor(
        ICdcRepository cdcRepository,
        IEventPublisher eventPublisher,
        IEventEnricher enricher,
        ILogger<CdcEventProcessor> logger,
        ICdcCheckpointManager checkpointManager)
        : base(logger)
    {
        _cdcRepository = cdcRepository ?? throw new ArgumentNullException(nameof(cdcRepository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _enricher = enricher ?? throw new ArgumentNullException(nameof(enricher));
        _checkpointManager = checkpointManager ?? throw new ArgumentNullException(nameof(checkpointManager));
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

        // Convert CDC events to platform events
        var events = changeEvents.Select(ConvertToBaseEvent).ToList();

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

    private BaseEvent ConvertToBaseEvent(dynamic changeEvent)
    {
        var evt = new BaseEvent
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri($"/sqlserver/{Environment.MachineName}/Hartonomous"),
            Type = GetEventType(changeEvent.Operation),
            Time = DateTimeOffset.UtcNow,
            Subject = $"{changeEvent.TableName}/lsn:{changeEvent.Lsn}",
            DataSchema = new Uri("https://schemas.microsoft.com/sqlserver/2025/ces"),
            Data = changeEvent.Data
        };

        // Add SQL Server specific extensions
        evt.Extensions["sqlserver"] = new Dictionary<string, object>
        {
            ["operation"] = GetOperationName(changeEvent.Operation),
            ["table"] = changeEvent.TableName,
            ["lsn"] = changeEvent.Lsn,
            ["database"] = "Hartonomous",
            ["server"] = Environment.MachineName
        };

        return evt;
    }

    private static string GetEventType(int operation) => operation switch
    {
        1 => "com.microsoft.sqlserver.cdc.delete",
        2 => "com.microsoft.sqlserver.cdc.insert",
        3 => "com.microsoft.sqlserver.cdc.update.before",
        4 => "com.microsoft.sqlserver.cdc.update.after",
        _ => "com.microsoft.sqlserver.cdc.unknown"
    };

    private static string GetOperationName(int operation) => operation switch
    {
        1 => "delete",
        2 => "insert",
        3 => "update_before",
        4 => "update_after",
        _ => "unknown"
    };
}

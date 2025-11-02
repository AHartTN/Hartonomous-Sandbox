using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Models;
using Microsoft.Extensions.Logging;

namespace CesConsumer.Services;

/// <summary>
/// Processes SQL Server Change Data Capture (CDC) events and publishes enriched events to messaging infrastructure.
/// Orchestrates retrieval, enrichment, publishing, and checkpointing of database change events.
/// </summary>
public class CdcEventProcessor
{
    private readonly ICdcRepository _cdcRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IEventEnricher _enricher;
    private readonly ILogger<CdcEventProcessor> _logger;
    private readonly ICdcCheckpointManager _checkpointManager;

    public CdcEventProcessor(
        ICdcRepository cdcRepository,
        IEventPublisher eventPublisher,
        IEventEnricher enricher,
        ILogger<CdcEventProcessor> logger,
        ICdcCheckpointManager checkpointManager)
    {
        _cdcRepository = cdcRepository ?? throw new ArgumentNullException(nameof(cdcRepository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _enricher = enricher ?? throw new ArgumentNullException(nameof(enricher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _checkpointManager = checkpointManager ?? throw new ArgumentNullException(nameof(checkpointManager));
    }

    public async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting CDC Consumer with event processing");

        var lastLsn = await _checkpointManager.GetLastProcessedLsnAsync(cancellationToken);
        _logger.LogInformation("Starting from LSN: {LastLsn}", lastLsn ?? "Beginning");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ProcessChangeEventsAsync(lastLsn, cancellationToken);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing change events");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private async Task ProcessChangeEventsAsync(string? lastLsn, CancellationToken cancellationToken)
    {
        var changeEvents = await _cdcRepository.GetChangeEventsSinceAsync(lastLsn, cancellationToken);
        
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
            _logger.LogInformation("Processed {Count} change events, new LSN: {MaxLsn}", 
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

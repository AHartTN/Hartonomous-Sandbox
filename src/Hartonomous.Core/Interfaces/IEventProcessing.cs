using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Interface for listening to change events from data sources
/// </summary>
public interface IEventListener
{
    /// <summary>
    /// Start listening for events
    /// </summary>
    Task StartListeningAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stop listening for events
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get the last processed sequence number
    /// </summary>
    Task<string?> GetLastProcessedSequenceAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Update the last processed sequence number
    /// </summary>
    Task UpdateLastProcessedSequenceAsync(string sequence, CancellationToken cancellationToken);
}

/// <summary>
/// Interface for processing raw events into structured format
/// </summary>
public interface IEventProcessor
{
    /// <summary>
    /// Process a batch of raw events
    /// </summary>
    Task<IEnumerable<CloudEvent>> ProcessEventsAsync(IEnumerable<ChangeEvent> rawEvents, CancellationToken cancellationToken);
}

/// <summary>
/// Interface for semantically enriching events with additional metadata
/// </summary>
public interface ISemanticEnricher
{
    /// <summary>
    /// Enrich a CloudEvent with semantic metadata
    /// </summary>
    Task EnrichEventAsync(CloudEvent cloudEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Interface for publishing events to external systems
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publish a batch of events
    /// </summary>
    Task PublishEventsAsync(IEnumerable<CloudEvent> events, CancellationToken cancellationToken);
}

/// <summary>
/// Represents a raw change event from the data source
/// </summary>
public class ChangeEvent
{
    public string Lsn { get; set; } = string.Empty;
    public int Operation { get; set; }
    public string TableName { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// CloudEvent representation following CNCF CloudEvents specification
/// </summary>
public class CloudEvent
{
    public string Id { get; set; } = string.Empty;
    public Uri Source { get; set; } = null!;
    public string Type { get; set; } = string.Empty;
    public DateTimeOffset Time { get; set; }
    public string? Subject { get; set; }
    public Uri? DataSchema { get; set; }
    public object? Data { get; set; }
    public Dictionary<string, object> Extensions { get; set; } = new();
}
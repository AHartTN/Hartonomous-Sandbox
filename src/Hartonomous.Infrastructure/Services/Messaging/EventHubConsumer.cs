using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hartonomous.Infrastructure.Services.Messaging;

/// <summary>
/// Azure Event Hubs implementation of IEventConsumer
/// Manages checkpointing and error handling automatically
/// </summary>
public sealed class EventHubConsumer : IEventConsumer, IAsyncDisposable
{
    private readonly EventProcessorClient _processorClient;
    private readonly ILogger<EventHubConsumer> _logger;
    private Func<object, CancellationToken, Task>? _eventHandler;

    public EventHubConsumer(
        IOptions<EventHubOptions> options,
        ILogger<EventHubConsumer> logger)
    {
        var config = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(config.ConnectionString))
        {
            throw new InvalidOperationException("Event Hub connection string is not configured");
        }

        if (string.IsNullOrWhiteSpace(config.Name))
        {
            throw new InvalidOperationException("Event Hub name is not configured");
        }

        var blobConnectionString = config.BlobStorageConnectionString 
            ?? throw new InvalidOperationException("Blob storage connection string is required for consumers");

        var blobServiceClient = new BlobServiceClient(blobConnectionString);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(config.BlobContainerName);

        _processorClient = new EventProcessorClient(
            blobContainerClient,
            config.ConsumerGroup,
            config.ConnectionString,
            config.Name);

        _processorClient.ProcessEventAsync += ProcessEventHandler;
        _processorClient.ProcessErrorAsync += ProcessErrorHandler;

        _logger.LogInformation("Event Hub Consumer initialized for {EventHubName} in consumer group {ConsumerGroup}",
            config.Name, config.ConsumerGroup);
    }

    public async Task StartAsync(Func<object, CancellationToken, Task> eventHandler, CancellationToken cancellationToken = default)
    {
        _eventHandler = eventHandler ?? throw new ArgumentNullException(nameof(eventHandler));
        
        await _processorClient.StartProcessingAsync(cancellationToken);
        _logger.LogInformation("Event Hub Consumer started processing");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _processorClient.StopProcessingAsync(cancellationToken);
        _logger.LogInformation("Event Hub Consumer stopped processing");
    }

    private async Task ProcessEventHandler(ProcessEventArgs eventArgs)
    {
        if (_eventHandler == null)
        {
            _logger.LogWarning("Received event but no handler is registered");
            return;
        }

        try
        {
            var eventData = eventArgs.Data;
            var body = eventData.Body.ToString();


            // Deserialize event
            var evt = JsonSerializer.Deserialize<object>(body);

            if (evt != null)
            {
                await _eventHandler(evt, eventArgs.CancellationToken);                // Update checkpoint after successful processing
                await eventArgs.UpdateCheckpointAsync(eventArgs.CancellationToken);
            }
            else
            {
                _logger.LogWarning("Received null or invalid event data");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event from partition {PartitionId}", 
                eventArgs.Partition.PartitionId);
            // Don't update checkpoint on error - event will be reprocessed
        }
    }

    private Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
    {
        _logger.LogError(eventArgs.Exception, 
            "Error in event processing for partition {PartitionId}, operation {Operation}",
            eventArgs.PartitionId, eventArgs.Operation);
        
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _processorClient.StopProcessingAsync();
        // EventProcessorClient is IDisposable in some versions, handle gracefully
        GC.SuppressFinalize(this);
        _logger.LogInformation("Event Hub Consumer disposed");
    }
}

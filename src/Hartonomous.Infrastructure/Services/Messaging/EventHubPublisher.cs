using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hartonomous.Infrastructure.Services.Messaging;

/// <summary>
/// Azure Event Hubs implementation of IEventPublisher
/// Thread-safe and supports batching for optimal performance
/// </summary>
public sealed class EventHubPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly EventHubProducerClient _producerClient;
    private readonly EventHubOptions _options;
    private readonly ILogger<EventHubPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public EventHubPublisher(
        IOptions<EventHubOptions> options,
        ILogger<EventHubPublisher> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("Event Hub connection string is not configured");
        }

        if (string.IsNullOrWhiteSpace(_options.Name))
        {
            throw new InvalidOperationException("Event Hub name is not configured");
        }

        _producerClient = new EventHubProducerClient(_options.ConnectionString, _options.Name);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        _logger.LogInformation("Event Hub Publisher initialized for {EventHubName}", _options.Name);
    }

    public async Task PublishAsync<TEvent>(TEvent evt, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));

        var eventData = CreateEventData(evt);
        
        await RetryAsync(async () =>
        {
            await _producerClient.SendAsync(new[] { eventData }, cancellationToken);
            _logger.LogDebug("Published single event to Event Hub");
        }, cancellationToken);
    }

    public async Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        if (events == null) throw new ArgumentNullException(nameof(events));

        var eventsList = events.ToList();
        if (eventsList.Count == 0) return;

        var batches = new List<EventDataBatch>();
        EventDataBatch? currentBatch = null;

        try
        {
            foreach (var evt in eventsList)
            {
                var eventData = CreateEventData(evt);

                // Create new batch if needed
                if (currentBatch == null)
                {
                    currentBatch = await _producerClient.CreateBatchAsync(cancellationToken);
                }

                // Try to add to current batch
                if (!currentBatch.TryAdd(eventData))
                {
                    // Batch is full, add to list and create new batch
                    batches.Add(currentBatch);
                    currentBatch = await _producerClient.CreateBatchAsync(cancellationToken);
                    
                    if (!currentBatch.TryAdd(eventData))
                    {
                        throw new InvalidOperationException("Event is too large to fit in a batch");
                    }
                }
            }

            // Add the last batch if it has events
            if (currentBatch != null && currentBatch.Count > 0)
            {
                batches.Add(currentBatch);
            }

            // Send all batches
            foreach (var batch in batches)
            {
                await RetryAsync(async () =>
                {
                    await _producerClient.SendAsync(batch, cancellationToken);
                }, cancellationToken);
            }

            _logger.LogInformation("Published {EventCount} events in {BatchCount} batches",
                eventsList.Count, batches.Count);
        }
        finally
        {
            // Dispose all batches
            foreach (var batch in batches)
            {
                batch?.Dispose();
            }
        }
    }

    private EventData CreateEventData<TEvent>(TEvent evt) where TEvent : class
    {
        var json = JsonSerializer.Serialize(evt, _jsonOptions);
        var eventData = new EventData(json);
        eventData.ContentType = "application/cloudevents+json";
        return eventData;
    }

    private async Task RetryAsync(Func<Task> operation, CancellationToken cancellationToken)
    {
        var attempts = 0;
        var maxAttempts = _options.MaxRetryAttempts;

        while (true)
        {
            try
            {
                await operation();
                return;
            }
            catch (Exception ex) when (attempts < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                attempts++;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempts)); // Exponential backoff
                
                _logger.LogWarning(ex, "Event Hub publish failed, attempt {Attempt}/{MaxAttempts}. Retrying in {Delay}s...",
                    attempts, maxAttempts, delay.TotalSeconds);
                
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _producerClient.CloseAsync();
        await _producerClient.DisposeAsync();
        _logger.LogInformation("Event Hub Publisher disposed");
    }
}

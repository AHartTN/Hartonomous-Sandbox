using System.Collections.Concurrent;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hartonomous.Infrastructure.Messaging;

/// <summary>
/// Options for Azure Service Bus event bus.
/// </summary>
public class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    /// <summary>
    /// Service Bus connection string or namespace endpoint.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Default topic name for events.
    /// </summary>
    public string DefaultTopic { get; set; } = "hartonomous-events";

    /// <summary>
    /// Max message size in bytes.
    /// </summary>
    public int MaxMessageSizeBytes { get; set; } = 256 * 1024; // 256 KB

    /// <summary>
    /// Enable dead-letter queue processing.
    /// </summary>
    public bool EnableDeadLetterQueue { get; set; } = true;

    /// <summary>
    /// Max delivery count before dead-lettering.
    /// </summary>
    public int MaxDeliveryCount { get; set; } = 10;
}

/// <summary>
/// Azure Service Bus implementation of IEventBus.
/// Uses topics and subscriptions for publish/subscribe messaging.
/// </summary>
public class ServiceBusEventBus : IEventBus, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusEventBus> _logger;
    private readonly ServiceBusOptions _options;
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();
    private readonly ConcurrentDictionary<string, ServiceBusProcessor> _processors = new();
    private readonly ConcurrentDictionary<string, Delegate> _handlers = new();
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public ServiceBusEventBus(
        ServiceBusClient client,
        IOptions<ServiceBusOptions> options,
        ILogger<ServiceBusEventBus> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(
        TEvent @event,
        string? topicName = null,
        CancellationToken cancellationToken = default) where TEvent : class
    {
        var topic = topicName ?? _options.DefaultTopic;
        var sender = GetOrCreateSender(topic);

        try
        {
            var body = JsonSerializer.SerializeToUtf8Bytes(@event, _jsonOptions);
            var message = new ServiceBusMessage(body)
            {
                Subject = typeof(TEvent).Name,
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString()
            };

            // Add custom properties for routing/filtering
            message.ApplicationProperties["EventType"] = typeof(TEvent).Name;
            if (@event is Events.IntegrationEvent integrationEvent)
            {
                message.ApplicationProperties["EventId"] = integrationEvent.EventId.ToString();
                message.ApplicationProperties["OccurredAt"] = integrationEvent.OccurredAt;
                if (integrationEvent.TenantId.HasValue)
                    message.ApplicationProperties["TenantId"] = integrationEvent.TenantId.Value;
                if (!string.IsNullOrEmpty(integrationEvent.CorrelationId))
                    message.CorrelationId = integrationEvent.CorrelationId;
            }

            await sender.SendMessageAsync(message, cancellationToken);
            
            _logger.LogInformation("Published {EventType} to topic '{Topic}'", 
                typeof(TEvent).Name, topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing {EventType} to topic '{Topic}'",
                typeof(TEvent).Name, topic);
            throw;
        }
    }

    public async Task PublishBatchAsync<TEvent>(
        IEnumerable<TEvent> events,
        string? topicName = null,
        CancellationToken cancellationToken = default) where TEvent : class
    {
        var topic = topicName ?? _options.DefaultTopic;
        var sender = GetOrCreateSender(topic);
        var eventList = events.ToList();

        try
        {
            using var batch = await sender.CreateMessageBatchAsync(cancellationToken);

            foreach (var @event in eventList)
            {
                var body = JsonSerializer.SerializeToUtf8Bytes(@event, _jsonOptions);
                var message = new ServiceBusMessage(body)
                {
                    Subject = typeof(TEvent).Name,
                    ContentType = "application/json",
                    MessageId = Guid.NewGuid().ToString()
                };

                message.ApplicationProperties["EventType"] = typeof(TEvent).Name;

                if (!batch.TryAddMessage(message))
                {
                    // Batch full, send current batch and create new one
                    await sender.SendMessagesAsync(batch, cancellationToken);
                    batch.Dispose();
                    
                    using var newBatch = await sender.CreateMessageBatchAsync(cancellationToken);
                    if (!newBatch.TryAddMessage(message))
                    {
                        _logger.LogWarning("Message too large for batch, sending individually");
                        await sender.SendMessageAsync(message, cancellationToken);
                    }
                }
            }

            if (batch.Count > 0)
            {
                await sender.SendMessagesAsync(batch, cancellationToken);
            }

            _logger.LogInformation("Published batch of {Count} events to topic '{Topic}'",
                eventList.Count, topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing batch to topic '{Topic}'", topic);
            throw;
        }
    }

    public Task SubscribeAsync<TEvent>(
        Func<TEvent, CancellationToken, Task> handler,
        string subscriptionName,
        CancellationToken cancellationToken = default) where TEvent : class
    {
        var topic = _options.DefaultTopic;
        var eventType = typeof(TEvent).Name;
        var processorKey = $"{topic}/{subscriptionName}";

        // Store handler for later use
        _handlers[processorKey] = handler;

        _logger.LogInformation("Registered subscription '{Subscription}' for event {EventType} on topic '{Topic}'",
            subscriptionName, eventType, topic);

        return Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Service Bus event bus processors");

        foreach (var kvp in _handlers)
        {
            var parts = kvp.Key.Split('/');
            var topic = parts[0];
            var subscription = parts[1];

            var processor = _client.CreateProcessor(topic, subscription, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 10,
                MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5),
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            });

            processor.ProcessMessageAsync += async args =>
            {
                try
                {
                    var eventType = args.Message.ApplicationProperties.ContainsKey("EventType")
                        ? args.Message.ApplicationProperties["EventType"].ToString()
                        : args.Message.Subject;

                    _logger.LogDebug("Processing message {MessageId} of type {EventType}",
                        args.Message.MessageId, eventType);

                    var handler = _handlers[kvp.Key];
                    var handlerType = handler.GetType().GetGenericArguments()[0];
                    var @event = JsonSerializer.Deserialize(args.Message.Body, handlerType, _jsonOptions);

                    if (@event != null && handler is Delegate typedHandler)
                    {
                        await (Task)typedHandler.DynamicInvoke(@event, args.CancellationToken)!;
                    }

                    await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message {MessageId}", args.Message.MessageId);

                    if (args.Message.DeliveryCount >= _options.MaxDeliveryCount)
                    {
                        await args.DeadLetterMessageAsync(args.Message, 
                            deadLetterReason: "MaxDeliveryCountExceeded",
                            deadLetterErrorDescription: ex.Message,
                            cancellationToken: args.CancellationToken);
                    }
                    else
                    {
                        await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
                    }
                }
            };

            processor.ProcessErrorAsync += args =>
            {
                _logger.LogError(args.Exception, "Error in Service Bus processor for {EntityPath}",
                    args.EntityPath);
                return Task.CompletedTask;
            };

            await processor.StartProcessingAsync(cancellationToken);
            _processors[kvp.Key] = processor;

            _logger.LogInformation("Started processor for topic '{Topic}' subscription '{Subscription}'",
                topic, subscription);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Service Bus event bus processors");

        foreach (var processor in _processors.Values)
        {
            await processor.StopProcessingAsync(cancellationToken);
            await processor.DisposeAsync();
        }

        _processors.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();

        foreach (var sender in _senders.Values)
        {
            await sender.DisposeAsync();
        }

        _senders.Clear();
        await _client.DisposeAsync();
    }

    private ServiceBusSender GetOrCreateSender(string topic)
    {
        return _senders.GetOrAdd(topic, t => _client.CreateSender(t));
    }
}

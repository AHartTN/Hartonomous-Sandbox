using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Messaging;

/// <summary>
/// In-memory event bus implementation for development/testing.
/// For production, use ServiceBusEventBus with Azure Service Bus.
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly ILogger<InMemoryEventBus> _logger;
    private readonly ConcurrentDictionary<string, List<Delegate>> _handlers = new();

    public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(
        TEvent @event,
        string? topicName = null,
        CancellationToken cancellationToken = default) where TEvent : class
    {
        var topic = topicName ?? typeof(TEvent).Name;
        
        _logger.LogInformation("Publishing event to topic '{Topic}': {EventType}", 
            topic, typeof(TEvent).Name);

        if (_handlers.TryGetValue(topic, out var handlers))
        {
            foreach (var handler in handlers)
            {
                if (handler is Func<TEvent, CancellationToken, Task> typedHandler)
                {
                    // Fire and forget (async execution)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await typedHandler(@event, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error handling event {EventType}", typeof(TEvent).Name);
                        }
                    }, cancellationToken);
                }
            }
        }

        return Task.CompletedTask;
    }

    public async Task PublishBatchAsync<TEvent>(
        IEnumerable<TEvent> events,
        string? topicName = null,
        CancellationToken cancellationToken = default) where TEvent : class
    {
        foreach (var @event in events)
        {
            await PublishAsync(@event, topicName, cancellationToken);
        }
    }

    public Task SubscribeAsync<TEvent>(
        Func<TEvent, CancellationToken, Task> handler,
        string subscriptionName,
        CancellationToken cancellationToken = default) where TEvent : class
    {
        var topic = typeof(TEvent).Name;
        
        _handlers.AddOrUpdate(
            topic,
            _ => new List<Delegate> { handler },
            (_, existing) =>
            {
                existing.Add(handler);
                return existing;
            });

        _logger.LogInformation("Subscribed to topic '{Topic}' with subscription '{Subscription}'",
            topic, subscriptionName);

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("In-memory event bus started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("In-memory event bus stopped");
        return Task.CompletedTask;
    }
}

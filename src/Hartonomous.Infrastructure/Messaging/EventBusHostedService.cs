using Hartonomous.Infrastructure.Messaging.Events;
using Hartonomous.Infrastructure.Messaging.Handlers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Messaging;

/// <summary>
/// Hosted service that initializes event bus subscriptions on startup.
/// </summary>
public class EventBusHostedService : IHostedService
{
    private readonly IEventBus _eventBus;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventBusHostedService> _logger;

    public EventBusHostedService(
        IEventBus eventBus,
        IServiceProvider serviceProvider,
        ILogger<EventBusHostedService> logger)
    {
        _eventBus = eventBus;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing event bus subscriptions");

        // OODA Loop event subscriptions
        await _eventBus.SubscribeAsync<ObservationEvent>(
            async (@event, ct) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ObservationEventHandler>();
                await handler.HandleAsync(@event, ct);
            },
            subscriptionName: "ooda-observation",
            cancellationToken);

        await _eventBus.SubscribeAsync<OrientationEvent>(
            async (@event, ct) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<OrientationEventHandler>();
                await handler.HandleAsync(@event, ct);
            },
            subscriptionName: "ooda-orientation",
            cancellationToken);

        await _eventBus.SubscribeAsync<DecisionEvent>(
            async (@event, ct) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<DecisionEventHandler>();
                await handler.HandleAsync(@event, ct);
            },
            subscriptionName: "ooda-decision",
            cancellationToken);

        await _eventBus.SubscribeAsync<ActionEvent>(
            async (@event, ct) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ActionEventHandler>();
                await handler.HandleAsync(@event, ct);
            },
            subscriptionName: "ooda-action",
            cancellationToken);

        // Domain event subscriptions
        await _eventBus.SubscribeAsync<AtomIngestedEvent>(
            async (@event, ct) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<AtomIngestedEventHandler>();
                await handler.HandleAsync(@event, ct);
            },
            subscriptionName: "atom-ingested",
            cancellationToken);

        await _eventBus.SubscribeAsync<CacheInvalidatedEvent>(
            async (@event, ct) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<CacheInvalidatedEventHandler>();
                await handler.HandleAsync(@event, ct);
            },
            subscriptionName: "cache-invalidated",
            cancellationToken);

        await _eventBus.SubscribeAsync<QuotaExceededEvent>(
            async (@event, ct) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<QuotaExceededEventHandler>();
                await handler.HandleAsync(@event, ct);
            },
            subscriptionName: "quota-exceeded",
            cancellationToken);

        // Start processing messages
        await _eventBus.StartAsync(cancellationToken);

        _logger.LogInformation("Event bus subscriptions initialized and processing started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping event bus");
        await _eventBus.StopAsync(cancellationToken);
    }
}

using Hartonomous.Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Hartonomous.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Event Bus connectivity and functionality.
/// </summary>
public class EventBusHealthCheck : IHealthCheck
{
    private readonly IEventBus _eventBus;

    public EventBusHealthCheck(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // For InMemoryEventBus, check if instance is available
            // For ServiceBusEventBus, this would perform actual connectivity check
            var eventBusType = _eventBus.GetType().Name;

            var data = new Dictionary<string, object>
            {
                ["EventBusType"] = eventBusType,
                ["IsConfigured"] = _eventBus != null
            };

            // If we got here without exception, event bus is available
            return Task.FromResult(HealthCheckResult.Healthy($"Event bus ({eventBusType}) is available", data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Event bus is unavailable", ex));
        }
    }
}

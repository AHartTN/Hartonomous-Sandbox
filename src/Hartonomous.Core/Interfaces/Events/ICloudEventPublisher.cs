namespace Hartonomous.Core.Interfaces.Events;

public interface ICloudEventPublisher
{
    /// <summary>
    /// Publish a batch of events
    /// </summary>
    Task PublishEventsAsync(IEnumerable<CloudEvent> events, CancellationToken cancellationToken);
}

/// <summary>
/// Represents a raw change event from the data source
/// </summary>

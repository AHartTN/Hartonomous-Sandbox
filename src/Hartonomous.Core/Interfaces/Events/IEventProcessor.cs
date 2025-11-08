namespace Hartonomous.Core.Interfaces.Events;

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

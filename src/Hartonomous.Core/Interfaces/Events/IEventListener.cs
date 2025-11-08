namespace Hartonomous.Core.Interfaces.Events;

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

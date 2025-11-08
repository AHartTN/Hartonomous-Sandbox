namespace Hartonomous.Core.Interfaces.Events;

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

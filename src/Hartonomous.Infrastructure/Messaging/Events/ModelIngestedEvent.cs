namespace Hartonomous.Infrastructure.Messaging.Events;

/// <summary>
/// Event published when a model is ingested.
/// </summary>
public class ModelIngestedEvent : IntegrationEvent
{
    public required int ModelId { get; init; }
    public required string ModelName { get; init; }
    public required string Architecture { get; init; }
    public int LayerCount { get; init; }
    public long TotalParameters { get; init; }
}

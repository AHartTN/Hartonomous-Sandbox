namespace Hartonomous.Infrastructure.Messaging.Events;

/// <summary>
/// Event published when inference completes.
/// </summary>
public class InferenceCompletedEvent : IntegrationEvent
{
    public required long InferenceId { get; init; }
    public required int ModelId { get; init; }
    public required string Status { get; init; }
    public long DurationMs { get; init; }
    public string? ErrorMessage { get; init; }
}

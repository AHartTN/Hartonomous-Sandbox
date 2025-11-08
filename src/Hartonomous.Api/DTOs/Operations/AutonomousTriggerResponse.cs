namespace Hartonomous.Api.DTOs.Operations;

/// <summary>
/// Response from autonomous trigger
/// </summary>
public class AutonomousTriggerResponse
{
    public required Guid CycleId { get; set; }
    public required Dictionary<string, int> QueueDepths { get; set; }
    public int EstimatedDurationMs { get; set; }
    public required string Status { get; set; }
    public DateTime TriggeredAt { get; set; }
}

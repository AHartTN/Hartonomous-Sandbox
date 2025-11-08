namespace Hartonomous.Api.DTOs.Autonomy;

public class ActionResponse
{
    public required Guid AnalysisId { get; init; }
    public required int ExecutedActions { get; init; }
    public required int QueuedActions { get; init; }
    public required int FailedActions { get; init; }
    public required List<ActionResult> Results { get; init; }
    public required DateTime TimestampUtc { get; init; }
}

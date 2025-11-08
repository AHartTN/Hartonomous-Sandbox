namespace Hartonomous.Api.DTOs.Autonomy;

public class ActionOutcomeSummary
{
    public required int SuccessfulActions { get; init; }
    public required int RegressedActions { get; init; }
    public required int TotalActions { get; init; }
}

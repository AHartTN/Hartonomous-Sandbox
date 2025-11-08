namespace Hartonomous.Api.DTOs.Autonomy;

public class OodaCycleHistoryResponse
{
    public required List<OodaCycleRecord> Cycles { get; init; }
    public required int TotalCycles { get; init; }
    public required double AvgLatencyImprovement { get; init; }
}

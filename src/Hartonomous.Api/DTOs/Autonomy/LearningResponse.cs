namespace Hartonomous.Api.DTOs.Autonomy;

public class LearningResponse
{
    public required Guid AnalysisId { get; init; }
    public required bool LearningCycleComplete { get; init; }
    public required PerformanceMetrics PerformanceMetrics { get; init; }
    public required ActionOutcomeSummary ActionOutcomes { get; init; }
    public required List<ActionOutcome> Outcomes { get; init; }
    public required DateTime TimestampUtc { get; init; }
}

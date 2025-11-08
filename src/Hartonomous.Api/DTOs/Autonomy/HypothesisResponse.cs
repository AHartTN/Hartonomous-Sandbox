namespace Hartonomous.Api.DTOs.Autonomy;

public class HypothesisResponse
{
    public required Guid AnalysisId { get; init; }
    public required int HypothesesGenerated { get; init; }
    public required List<Hypothesis> Hypotheses { get; init; }
    public required DateTime TimestampUtc { get; init; }
}

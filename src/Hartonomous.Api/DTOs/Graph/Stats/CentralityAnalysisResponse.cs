namespace Hartonomous.Api.DTOs.Graph.Stats;

public class CentralityAnalysisResponse
{
    public required string Algorithm { get; set; }
    public required List<CentralityScore> CentralityScores { get; set; }
    public int TotalNodesAnalyzed { get; set; }
}

namespace Hartonomous.Api.DTOs.Graph.Stats;

public class CentralityAnalysisRequest
{
    public string Algorithm { get; set; } = "degree";
    public int? TopNodes { get; set; } = 100;
}

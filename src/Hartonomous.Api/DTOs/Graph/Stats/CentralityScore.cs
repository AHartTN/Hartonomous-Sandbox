namespace Hartonomous.Api.DTOs.Graph.Stats;

public class CentralityScore
{
    public long AtomId { get; set; }
    public double Score { get; set; }
    public int Rank { get; set; }
    public string? Modality { get; set; }
    public string? CanonicalText { get; set; }
}

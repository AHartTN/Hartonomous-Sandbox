namespace Hartonomous.Api.DTOs.Graph.Stats;

public class GraphStatsResponse
{
    public long TotalNodes { get; set; }
    public long TotalRelationships { get; set; }
    public required List<string> Modalities { get; set; }
    public required Dictionary<string, long> ModalityCounts { get; set; }
    public required List<string> RelationshipTypes { get; set; }
    public required Dictionary<string, long> RelationshipTypeCounts { get; set; }
    public double Density { get; set; }
    public long? ConnectedComponents { get; set; }
    public Dictionary<string, object>? ComponentDistribution { get; set; }
    public double AverageDegree { get; set; }
}

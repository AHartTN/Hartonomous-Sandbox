namespace Hartonomous.Api.DTOs.Graph.Stats;

public class GetGraphStatsResponse
{
    public long TotalNodes { get; set; }
    public long TotalRelationships { get; set; }
    public required Dictionary<string, long> NodesByModality { get; set; }
    public required Dictionary<string, long> RelationshipsByType { get; set; }
    public double AverageDegree { get; set; }
    public int MaxDegree { get; set; }
    public long IsolatedNodes { get; set; }
}

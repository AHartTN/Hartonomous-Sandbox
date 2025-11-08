namespace Hartonomous.Api.DTOs.Graph.Traversal;

public class GraphPath
{
    public required List<GraphNode> Nodes { get; set; }
    public required List<GraphRelationship> Relationships { get; set; }
    public int PathLength { get; set; }
    public double? TotalWeight { get; set; }
}

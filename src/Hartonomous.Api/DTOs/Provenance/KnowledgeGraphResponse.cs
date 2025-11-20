using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Provenance;


public class KnowledgeGraphResponse
{
    public long CenterAtomId { get; set; }
    public int Depth { get; set; }
    public List<GraphNode> Nodes { get; set; } = new();
    public List<GraphRelationship> Relationships { get; set; } = new();
    public List<GraphCommunity> Communities { get; set; } = new();
    public GraphStatistics Statistics { get; set; } = new();
    public bool DemoMode { get; set; }
}

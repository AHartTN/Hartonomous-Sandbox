namespace Hartonomous.Api.DTOs.Graph.Traversal;

public class GraphRelationship
{
    public required string Type { get; set; }
    public long FromAtomId { get; set; }
    public long ToAtomId { get; set; }
    public double? Weight { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}

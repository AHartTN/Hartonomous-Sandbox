namespace Hartonomous.Api.DTOs.Graph.Traversal;

public class GraphNode
{
    public long AtomId { get; set; }
    public required string Modality { get; set; }
    public string? CanonicalText { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}

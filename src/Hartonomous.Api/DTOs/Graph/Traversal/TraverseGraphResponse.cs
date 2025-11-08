namespace Hartonomous.Api.DTOs.Graph.Traversal;

public class TraverseGraphResponse
{
    public long StartAtomId { get; set; }
    public long? EndAtomId { get; set; }
    public required List<GraphPath> Paths { get; set; }
    public int TotalPathsFound { get; set; }
}

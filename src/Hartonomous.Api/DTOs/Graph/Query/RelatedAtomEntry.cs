namespace Hartonomous.Api.DTOs.Graph.Query;

public class RelatedAtomEntry
{
    public long AtomId { get; set; }
    public required string Modality { get; set; }
    public string? CanonicalText { get; set; }
    public required string RelationshipType { get; set; }
    public double? Similarity { get; set; }
    public int Depth { get; set; }
    public required List<string> PathDescription { get; set; }
}

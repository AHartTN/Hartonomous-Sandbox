namespace Hartonomous.Api.DTOs.Graph.Traversal;

public class CreateRelationshipResponse
{
    public long FromAtomId { get; set; }
    public long ToAtomId { get; set; }
    public required string RelationshipType { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
}

namespace Hartonomous.Api.DTOs.Graph.Query;

public class ConceptRelationship
{
    public long FromAtomId { get; set; }
    public long ToAtomId { get; set; }
    public required string Type { get; set; }
    public double? Strength { get; set; }
}

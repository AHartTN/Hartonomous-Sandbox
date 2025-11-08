namespace Hartonomous.Api.DTOs.Graph.Query;

public class ExploreConceptResponse
{
    public required string ConceptText { get; set; }
    public required List<ConceptNode> Nodes { get; set; }
    public required List<ConceptRelationship> Relationships { get; set; }
    public required Dictionary<string, int> ModalityBreakdown { get; set; }
}

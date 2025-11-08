namespace Hartonomous.Api.DTOs.Graph.Stats;

public class RelationshipAnalysisResponse
{
    public string? ModalityFilter { get; set; }
    public required List<RelationshipStats> RelationshipStats { get; set; }
    public List<CrossModalityStats>? CrossModalityStats { get; set; }
    public long TotalRelationshipsAnalyzed { get; set; }
}

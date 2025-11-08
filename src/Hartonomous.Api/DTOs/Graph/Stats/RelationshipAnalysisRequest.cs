namespace Hartonomous.Api.DTOs.Graph.Stats;

public class RelationshipAnalysisRequest
{
    public string? ModalityFilter { get; set; }
    public int? TopRelationships { get; set; } = 20;
}

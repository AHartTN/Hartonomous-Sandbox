namespace Hartonomous.Api.DTOs.Graph.Stats;

public class CrossModalityStats
{
    public required string SourceModality { get; set; }
    public required string TargetModality { get; set; }
    public required string RelationshipType { get; set; }
    public long Count { get; set; }
}

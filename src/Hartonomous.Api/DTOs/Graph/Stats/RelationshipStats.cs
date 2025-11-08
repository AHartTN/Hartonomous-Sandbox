namespace Hartonomous.Api.DTOs.Graph.Stats;

public class RelationshipStats
{
    public required string RelationshipType { get; set; }
    public long Count { get; set; }
    public double? AverageWeight { get; set; }
    public double? MinWeight { get; set; }
    public double? MaxWeight { get; set; }
    public double? WeightStdDev { get; set; }
    public required List<string> SourceModalities { get; set; }
    public required List<string> TargetModalities { get; set; }
}

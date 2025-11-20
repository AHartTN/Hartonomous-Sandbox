namespace Hartonomous.Api.DTOs.Provenance;

public record PathStatistics
{
    public int TotalPaths { get; init; }
    public int PathsTaken { get; init; }
    public int PathsPruned { get; init; }
    public int DecisionPoints { get; init; }
    public double AverageConfidence { get; init; }
    public required string SpatialDistance { get; init; }
}

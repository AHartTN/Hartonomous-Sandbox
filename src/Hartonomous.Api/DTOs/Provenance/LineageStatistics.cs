namespace Hartonomous.Api.DTOs.Provenance;

public record LineageStatistics
{
    public int TotalNodes { get; init; }
    public int TotalEdges { get; init; }
    public int MaxDepthReached { get; init; }
    public required string SpatialCoverage { get; init; }
    public required string TemporalSpan { get; init; }
}

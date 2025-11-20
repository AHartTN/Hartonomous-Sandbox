namespace Hartonomous.Api.DTOs.MLOps;

public record ErrorStatistics
{
    public int TotalErrors { get; init; }
    public int ClustersFound { get; init; }
    public int AverageClusterSize { get; init; }
    public required string SpatialSpread { get; init; }
    public required string TemporalWindow { get; init; }
    public required string MostCommonErrorType { get; init; }
}

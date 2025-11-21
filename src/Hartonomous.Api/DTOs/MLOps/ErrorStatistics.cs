namespace Hartonomous.Api.DTOs.MLOps;

public record ErrorStatistics
{
    public long TotalErrors { get; init; }
    public int ClustersFound { get; init; }
    public double AverageClusterSize { get; init; }
    public required string SpatialSpread { get; init; }
    public required string TemporalWindow { get; init; }
    public required string MostCommonErrorType { get; init; }
}

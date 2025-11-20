using Hartonomous.Api.DTOs.Provenance;

namespace Hartonomous.Api.DTOs.MLOps;

public record ErrorClustersResponse
{
    public long? SessionFilter { get; init; }
    public int MinClusterSize { get; init; }
    public required List<ErrorCluster> Clusters { get; init; }
    public required SpatialVisualizationData SpatialHeatmap { get; init; }
    public required ErrorStatistics Statistics { get; init; }
    public bool DemoMode { get; init; }
}

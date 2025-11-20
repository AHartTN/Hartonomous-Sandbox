using Hartonomous.Api.DTOs.Reasoning;

namespace Hartonomous.Api.DTOs.Provenance;

public record SessionPathsResponse
{
    public long SessionId { get; init; }
    public required List<ReasoningPath> Paths { get; init; }
    public required SpatialVisualizationData SpatialTraversal { get; init; }
    public required PathStatistics Statistics { get; init; }
    public bool DemoMode { get; init; }
}

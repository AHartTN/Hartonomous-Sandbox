using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Provenance;


public record AtomLineageResponse
{
    public long AtomId { get; init; }
    public int MaxDepth { get; init; }
    public required List<LineageNode> Nodes { get; init; }
    public required List<LineageEdge> Edges { get; init; }
    public required SpatialVisualizationData SpatialData { get; init; }
    public required LineageStatistics Statistics { get; init; }
    public bool DemoMode { get; init; }
}

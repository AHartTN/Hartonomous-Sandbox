using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Provenance;


public record InfluencingAtomsResponse
{
    public long ResultAtomId { get; init; }
    public double MinInfluenceThreshold { get; init; }
    public required List<InfluencingAtom> Influences { get; init; }
    public required SpatialVisualizationData SpatialDistribution { get; init; }
    public required InfluenceStatistics Statistics { get; init; }
    public bool DemoMode { get; init; }
}

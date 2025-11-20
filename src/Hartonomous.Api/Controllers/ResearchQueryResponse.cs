using System.Collections.Generic;

namespace Hartonomous.Api.Controllers;

public class ResearchQueryResponse
{
    public string Query { get; set; } = string.Empty;
    public int ExecutionTimeMs { get; set; }
    public List<ResearchResult> Results { get; set; } = new();
    public QueryAggregations Aggregations { get; set; } = new();
    public SpatialBounds SpatialCoverage { get; set; } = new();
    public bool DemoMode { get; set; }
}

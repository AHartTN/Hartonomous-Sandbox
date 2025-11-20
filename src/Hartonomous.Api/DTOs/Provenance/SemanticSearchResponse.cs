using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Provenance;


public class SemanticSearchResponse
{
    public string Query { get; set; } = string.Empty;
    public int Limit { get; set; }
    public double MinSimilarity { get; set; }
    public int ExecutionTimeMs { get; set; }
    public List<SemanticMatch> Matches { get; set; } = new();
    public SearchStatistics Statistics { get; set; } = new();
    public bool DemoMode { get; set; }
}

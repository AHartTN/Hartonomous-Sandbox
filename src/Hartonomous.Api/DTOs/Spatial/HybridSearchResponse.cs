namespace Hartonomous.Api.DTOs.Spatial;

public sealed class HybridSearchResponse
{
    public required List<SpatialSearchResult> Results { get; set; }
    public int TotalResults { get; set; }
    public long QueryTimeMs { get; set; }
    public string? PerformanceProfile { get; set; }
}

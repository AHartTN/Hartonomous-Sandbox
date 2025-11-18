namespace Hartonomous.Api.DTOs.Spatial;

public sealed class FusionSearchResponse
{
    public required List<FusionSearchResult> Results { get; set; }
    public int TotalResults { get; set; }
    public required FusionWeights Weights { get; set; }
}

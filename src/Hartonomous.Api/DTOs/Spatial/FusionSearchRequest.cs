namespace Hartonomous.Api.DTOs.Spatial;

public sealed class FusionSearchRequest
{
    public required float[] QueryVector { get; set; }
    public string? Keywords { get; set; }
    public List<SpatialCoordinate>? SpatialRegion { get; set; }
    public int? TopK { get; set; }
    public double? VectorWeight { get; set; }
    public double? KeywordWeight { get; set; }
    public double? SpatialWeight { get; set; }
    public int? TenantId { get; set; }
}

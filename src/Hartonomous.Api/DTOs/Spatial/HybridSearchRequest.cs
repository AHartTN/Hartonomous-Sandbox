namespace Hartonomous.Api.DTOs.Spatial;

public sealed class HybridSearchRequest
{
    public required float[] QueryVector { get; set; }
    public required SpatialCoordinate SpatialQuery { get; set; }
    public int? SpatialCandidates { get; set; }
    public int? TopK { get; set; }
    public string? DistanceMetric { get; set; }
    public string? EmbeddingType { get; set; }
    public int? ModelId { get; set; }
    public int? TenantId { get; set; }
}

namespace Hartonomous.Api.DTOs.Spatial;

public sealed class FusionSearchResult
{
    public long AtomId { get; set; }
    public double VectorScore { get; set; }
    public double KeywordScore { get; set; }
    public double SpatialScore { get; set; }
    public double CombinedScore { get; set; }
    public required string ContentHash { get; set; }
    public string? ContentType { get; set; }
    public DateTime CreatedAt { get; set; }
}

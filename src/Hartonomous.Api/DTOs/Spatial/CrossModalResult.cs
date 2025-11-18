namespace Hartonomous.Api.DTOs.Spatial;

public sealed class CrossModalResult
{
    public long AtomEmbeddingId { get; set; }
    public long AtomId { get; set; }
    public required string Modality { get; set; }
    public string? Subtype { get; set; }
    public string? CanonicalText { get; set; }
    public double? SpatialDistance { get; set; }
}

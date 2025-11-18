namespace Hartonomous.Api.DTOs.Spatial;

public sealed class SpatialSearchResult
{
    public long AtomEmbeddingId { get; set; }
    public long AtomId { get; set; }
    public required string Modality { get; set; }
    public string? Subtype { get; set; }
    public string? EmbeddingType { get; set; }
    public int? ModelId { get; set; }
    public double ExactDistance { get; set; }
    public double SpatialDistance { get; set; }
}

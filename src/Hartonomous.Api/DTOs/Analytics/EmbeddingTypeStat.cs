namespace Hartonomous.Api.DTOs.Analytics;

public class EmbeddingTypeStat
{
    public required string EmbeddingType { get; set; }
    public int? ModelId { get; set; }
    public string? ModelName { get; set; }
    public long TotalEmbeddings { get; set; }
    public long UniqueAtoms { get; set; }
    public int? AvgDimension { get; set; }
    public long UsePaddingCount { get; set; }
    public long ComponentStorageCount { get; set; }
    public double AvgSpatialDistance { get; set; }
}

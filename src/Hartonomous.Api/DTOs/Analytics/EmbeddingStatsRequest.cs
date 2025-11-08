namespace Hartonomous.Api.DTOs.Analytics;

public class EmbeddingStatsRequest
{
    public string? EmbeddingType { get; set; }
    public int? ModelId { get; set; }
}

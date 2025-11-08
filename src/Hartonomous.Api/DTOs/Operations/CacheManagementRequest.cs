namespace Hartonomous.Api.DTOs.Operations;

public class CacheManagementRequest
{
    public string Operation { get; set; } = "clear"; // clear, warm, stats
    public string? CacheType { get; set; } // vector, model, embedding, all
    public int? ModelId { get; set; }
}

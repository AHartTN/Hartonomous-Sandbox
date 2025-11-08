namespace Hartonomous.Api.DTOs.Analytics;

public class StorageMetricsResponse
{
    public long TotalAtoms { get; set; }
    public long TotalEmbeddings { get; set; }
    public long TotalTensorAtoms { get; set; }
    public long TotalModels { get; set; }
    public long TotalLayers { get; set; }
    public long TotalInferenceRequests { get; set; }
    public StorageSizeBreakdown SizeBreakdown { get; set; } = new();
    public DeduplicationMetrics Deduplication { get; set; } = new();
}

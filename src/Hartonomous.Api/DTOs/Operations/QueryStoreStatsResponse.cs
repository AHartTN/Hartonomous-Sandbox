namespace Hartonomous.Api.DTOs.Operations;

public class QueryStoreStatsResponse
{
    public bool QueryStoreEnabled { get; set; }
    public required string OperationMode { get; set; }
    public long TotalQueries { get; set; }
    public long TotalPlans { get; set; }
    public long CurrentStorageMB { get; set; }
    public long MaxStorageMB { get; set; }
    public double StorageUsedPercent { get; set; }
    public required List<TopQueryEntry> TopQueries { get; set; }
}

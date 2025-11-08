namespace Hartonomous.Api.DTOs.Analytics;

public class StorageSizeBreakdown
{
    public long AtomTableSizeMB { get; set; }
    public long EmbeddingTableSizeMB { get; set; }
    public long TensorAtomTableSizeMB { get; set; }
    public long FilestreamSizeMB { get; set; }
    public long TotalDatabaseSizeMB { get; set; }
}

namespace Hartonomous.Api.DTOs.Bulk;

public class BulkJobItemResult
{
    public int ItemIndex { get; set; }
    public required string Status { get; set; }
    public long? AtomId { get; set; }
    public string? ContentHash { get; set; }
    public bool IsDuplicate { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan? ProcessingTime { get; set; }
}

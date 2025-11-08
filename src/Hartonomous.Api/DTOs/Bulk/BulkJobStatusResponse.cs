namespace Hartonomous.Api.DTOs.Bulk;

public class BulkJobStatusResponse
{
    public required string JobId { get; set; }
    public required string Status { get; set; } // pending, processing, completed, failed, cancelled
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int SuccessItems { get; set; }
    public int FailedItems { get; set; }
    public double ProgressPercentage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? TotalDuration { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public required List<BulkJobItemResult> Results { get; set; }
    public string? ErrorMessage { get; set; }
}

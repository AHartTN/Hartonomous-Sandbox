namespace Hartonomous.Api.DTOs.Bulk;

public class BulkJobSummary
{
    public required string JobId { get; set; }
    public required string Status { get; set; }
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public double ProgressPercentage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
}

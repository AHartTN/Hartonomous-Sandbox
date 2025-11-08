namespace Hartonomous.Api.DTOs.Bulk;

public class BulkIngestResponse
{
    public required string JobId { get; set; }
    public required string Status { get; set; }
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int FailedItems { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CallbackUrl { get; set; }
    public string? StatusUrl { get; set; }
}

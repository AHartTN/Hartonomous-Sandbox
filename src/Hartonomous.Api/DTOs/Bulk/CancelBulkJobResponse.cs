namespace Hartonomous.Api.DTOs.Bulk;

public class CancelBulkJobResponse
{
    public required string JobId { get; set; }
    public bool Success { get; set; }
    public required string Status { get; set; }
    public int ItemsProcessedBeforeCancellation { get; set; }
    public string? Message { get; set; }
}

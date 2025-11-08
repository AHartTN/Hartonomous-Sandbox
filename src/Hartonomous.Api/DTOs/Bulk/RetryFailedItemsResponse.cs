namespace Hartonomous.Api.DTOs.Bulk;

public class RetryFailedItemsResponse
{
    public required string NewJobId { get; set; }
    public int ItemsToRetry { get; set; }
    public required string Status { get; set; }
}

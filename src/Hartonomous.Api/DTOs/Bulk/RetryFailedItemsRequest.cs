using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Bulk;

public class RetryFailedItemsRequest
{
    [Required]
    public required string JobId { get; set; }

    public bool OnlyRetryFailed { get; set; } = true;
}

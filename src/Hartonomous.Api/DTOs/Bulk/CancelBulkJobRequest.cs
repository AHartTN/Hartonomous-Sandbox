using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Bulk;

public class CancelBulkJobRequest
{
    [Required]
    public required string JobId { get; set; }

    public string? Reason { get; set; }
}

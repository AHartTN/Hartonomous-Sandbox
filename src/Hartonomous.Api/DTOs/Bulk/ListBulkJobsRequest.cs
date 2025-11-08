using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Bulk;

public class ListBulkJobsRequest
{
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public int PageNumber { get; set; } = 1;

    public string? Status { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

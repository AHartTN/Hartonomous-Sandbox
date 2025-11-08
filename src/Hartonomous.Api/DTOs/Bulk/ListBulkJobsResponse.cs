namespace Hartonomous.Api.DTOs.Bulk;

public class ListBulkJobsResponse
{
    public required List<BulkJobSummary> Jobs { get; set; }
    public int TotalJobs { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

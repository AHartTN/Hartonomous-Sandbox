namespace Hartonomous.Api.DTOs.Billing;

public class UsageReportRequest
{
    public int? TenantId { get; set; }
    public string ReportType { get; set; } = "Summary"; // Summary, Detailed, Forecast
    public string TimeRange { get; set; } = "Month"; // Day, Week, Month, Year
}

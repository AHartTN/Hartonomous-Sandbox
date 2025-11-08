namespace Hartonomous.Api.DTOs.Analytics;

public class UsageAnalyticsResponse
{
    public required List<UsageDataPoint> DataPoints { get; set; }
    public UsageSummary Summary { get; set; } = new();
}

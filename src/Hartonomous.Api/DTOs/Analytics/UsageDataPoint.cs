namespace Hartonomous.Api.DTOs.Analytics;

public class UsageDataPoint
{
    public DateTime Timestamp { get; set; }
    public long TotalRequests { get; set; }
    public long UniqueAtoms { get; set; }
    public long DeduplicatedCount { get; set; }
    public double DeduplicationRate { get; set; }
    public long TotalBytesProcessed { get; set; }
    public double AvgResponseTimeMs { get; set; }
}

namespace Hartonomous.Api.DTOs.Analytics
{
    public class UsageSummary
    {
        public long TotalRequests { get; set; }
        public long TotalAtoms { get; set; }
        public long TotalDeduped { get; set; }
        public double OverallDeduplicationRate { get; set; }
        public long TotalBytesProcessed { get; set; }
        public double AvgResponseTimeMs { get; set; }
    }
}

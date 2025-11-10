namespace Hartonomous.Api.DTOs.Billing
{
    public class QuotaResponse
    {
        public int TenantId { get; set; }
        public required string UsageType { get; set; }
        public long QuotaLimit { get; set; }
        public long CurrentUsage { get; set; }
        public decimal UsagePercent { get; set; }
        public bool IsActive { get; set; }
    }
}

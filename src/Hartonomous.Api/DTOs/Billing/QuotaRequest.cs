namespace Hartonomous.Api.DTOs.Billing
{
    public class QuotaRequest
    {
        public required int TenantId { get; set; }
        public required string UsageType { get; set; }
        public required long QuotaLimit { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

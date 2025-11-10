namespace Hartonomous.Api.DTOs.Billing
{
    public class RecordUsageResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public bool QuotaExceeded { get; set; }
    }
}

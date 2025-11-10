namespace Hartonomous.Api.DTOs.Billing
{
    public class UsageBreakdownItem
    {
        public required string UsageType { get; set; }
        public long TotalQuantity { get; set; }
        public decimal TotalCost { get; set; }
    }
}

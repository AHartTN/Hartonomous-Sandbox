namespace Hartonomous.Api.DTOs.Billing;

public class UsageTypeSummary
{
    public required string UsageType { get; set; }
    public long TotalQuantity { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AvgQuantity { get; set; }
    public int RecordCount { get; set; }
}

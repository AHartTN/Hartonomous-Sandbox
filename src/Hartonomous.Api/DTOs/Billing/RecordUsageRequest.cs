namespace Hartonomous.Api.DTOs.Billing;

public class RecordUsageRequest
{
    public required int TenantId { get; set; }
    public required string UsageType { get; set; } // TokenUsage, StorageUsage, VectorSearch, ComputeUsage
    public required long Quantity { get; set; }
    public required string UnitType { get; set; } // Tokens, Bytes, Queries, MilliCoreSeconds
    public decimal? CostPerUnit { get; set; }
    public string? Metadata { get; set; }
}

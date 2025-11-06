namespace Hartonomous.Api.DTOs.Billing;

public class UsageReportRequest
{
    public int? TenantId { get; set; }
    public string ReportType { get; set; } = "Summary"; // Summary, Detailed, Forecast
    public string TimeRange { get; set; } = "Month"; // Day, Week, Month, Year
}

public class UsageReportResponse
{
    public required List<UsageTypeSummary> UsageSummaries { get; set; }
}

public class UsageTypeSummary
{
    public required string UsageType { get; set; }
    public long TotalQuantity { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AvgQuantity { get; set; }
    public int RecordCount { get; set; }
}

public class CalculateBillRequest
{
    public required int TenantId { get; set; }
    public DateTime? BillingPeriodStart { get; set; }
    public DateTime? BillingPeriodEnd { get; set; }
    public bool GenerateInvoice { get; set; }
}

public class BillCalculationResponse
{
    public int TenantId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public List<UsageBreakdownItem>? UsageBreakdown { get; set; }
}

public class UsageBreakdownItem
{
    public required string UsageType { get; set; }
    public long TotalQuantity { get; set; }
    public decimal TotalCost { get; set; }
}

public class RecordUsageRequest
{
    public required int TenantId { get; set; }
    public required string UsageType { get; set; } // TokenUsage, StorageUsage, VectorSearch, ComputeUsage
    public required long Quantity { get; set; }
    public required string UnitType { get; set; } // Tokens, Bytes, Queries, MilliCoreSeconds
    public decimal? CostPerUnit { get; set; }
    public string? Metadata { get; set; }
}

public class RecordUsageResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public bool QuotaExceeded { get; set; }
}

public class QuotaRequest
{
    public required int TenantId { get; set; }
    public required string UsageType { get; set; }
    public required long QuotaLimit { get; set; }
    public bool IsActive { get; set; } = true;
}

public class QuotaResponse
{
    public int TenantId { get; set; }
    public required string UsageType { get; set; }
    public long QuotaLimit { get; set; }
    public long CurrentUsage { get; set; }
    public decimal UsagePercent { get; set; }
    public bool IsActive { get; set; }
}

public class InvoiceResponse
{
    public long InvoiceId { get; set; }
    public required string InvoiceNumber { get; set; }
    public int TenantId { get; set; }
    public DateTime BillingPeriodStart { get; set; }
    public DateTime BillingPeriodEnd { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public required string Status { get; set; } // Pending, Paid, Overdue, Cancelled
    public DateTime GeneratedUtc { get; set; }
    public DateTime? PaidUtc { get; set; }
}

namespace Hartonomous.Api.DTOs.Billing;

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

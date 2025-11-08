namespace Hartonomous.Api.DTOs.Billing;

public class CalculateBillRequest
{
    public required int TenantId { get; set; }
    public DateTime? BillingPeriodStart { get; set; }
    public DateTime? BillingPeriodEnd { get; set; }
    public bool GenerateInvoice { get; set; }
}

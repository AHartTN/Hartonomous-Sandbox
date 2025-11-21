using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface IBillingInvoice
{
    long InvoiceId { get; set; }
    int TenantId { get; set; }
    string InvoiceNumber { get; set; }
    DateTime BillingPeriodStart { get; set; }
    DateTime BillingPeriodEnd { get; set; }
    decimal Subtotal { get; set; }
    decimal Discount { get; set; }
    decimal Tax { get; set; }
    decimal Total { get; set; }
    string Status { get; set; }
    DateTime GeneratedUtc { get; set; }
    DateTime? PaidUtc { get; set; }
    string? MetadataJson { get; set; }
    string? StripeInvoiceId { get; set; }
    string? StripeStatus { get; set; }
    string? StripePdfUrl { get; set; }
    string? StripeHostedUrl { get; set; }
    ICollection<BillingCredit> BillingCredits { get; set; }
}

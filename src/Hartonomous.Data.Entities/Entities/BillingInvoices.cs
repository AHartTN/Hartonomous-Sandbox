using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class BillingInvoices : IBillingInvoices
{
    public long InvoiceId { get; set; }

    public int TenantId { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public DateTime BillingPeriodStart { get; set; }

    public DateTime BillingPeriodEnd { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Discount { get; set; }

    public decimal Tax { get; set; }

    public decimal Total { get; set; }

    public string Status { get; set; } = null!;

    public DateTime GeneratedUtc { get; set; }

    public DateTime? PaidUtc { get; set; }

    public string? MetadataJson { get; set; }
}

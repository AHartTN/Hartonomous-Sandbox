using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public partial class BillingUsageLedger : IBillingUsageLedger
{
    public long LedgerId { get; set; }

    public string TenantId { get; set; } = null!;

    public string? PrincipalId { get; set; }

    public string? Operation { get; set; }

    public string? MessageType { get; set; }

    public string? Handler { get; set; }

    public string? UsageType { get; set; }

    public long? Quantity { get; set; }

    public string? UnitType { get; set; }

    public decimal? CostPerUnit { get; set; }

    public decimal? Units { get; set; }

    public decimal? BaseRate { get; set; }

    public decimal Multiplier { get; set; }

    public decimal TotalCost { get; set; }

    public string? Metadata { get; set; }

    public string? MetadataJson { get; set; }

    public DateTime? RecordedUtc { get; set; }

    public DateTime TimestampUtc { get; set; }
}

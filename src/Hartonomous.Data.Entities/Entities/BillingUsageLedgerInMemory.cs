using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class BillingUsageLedgerInMemory : IBillingUsageLedgerInMemory
{
    public long LedgerId { get; set; }

    public string TenantId { get; set; } = null!;

    public string PrincipalId { get; set; } = null!;

    public string Operation { get; set; } = null!;

    public string? MessageType { get; set; }

    public string? Handler { get; set; }

    public decimal Units { get; set; }

    public decimal BaseRate { get; set; }

    public decimal Multiplier { get; set; }

    public decimal TotalCost { get; set; }

    public string? MetadataJson { get; set; }

    public DateTime TimestampUtc { get; set; }
}

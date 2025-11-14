using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IBillingUsageLedger
{
    long LedgerId { get; set; }
    string TenantId { get; set; }
    string? PrincipalId { get; set; }
    string? Operation { get; set; }
    string? MessageType { get; set; }
    string? Handler { get; set; }
    string? UsageType { get; set; }
    long? Quantity { get; set; }
    string? UnitType { get; set; }
    decimal? CostPerUnit { get; set; }
    decimal? Units { get; set; }
    decimal? BaseRate { get; set; }
    decimal Multiplier { get; set; }
    decimal TotalCost { get; set; }
    string? Metadata { get; set; }
    string? MetadataJson { get; set; }
    DateTime? RecordedUtc { get; set; }
    DateTime TimestampUtc { get; set; }
}

using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IBillingUsageLedgerInMemory
{
    long LedgerId { get; set; }
    string TenantId { get; set; }
    string PrincipalId { get; set; }
    string Operation { get; set; }
    string? MessageType { get; set; }
    string? Handler { get; set; }
    decimal Units { get; set; }
    decimal BaseRate { get; set; }
    decimal Multiplier { get; set; }
    decimal TotalCost { get; set; }
    string? MetadataJson { get; set; }
    DateTime TimestampUtc { get; set; }
}

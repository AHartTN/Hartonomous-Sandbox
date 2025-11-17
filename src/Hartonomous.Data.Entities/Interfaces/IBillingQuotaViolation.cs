using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface IBillingQuotaViolation
{
    long ViolationId { get; set; }
    int TenantId { get; set; }
    string UsageType { get; set; }
    long QuotaLimit { get; set; }
    long CurrentUsage { get; set; }
    DateTime ViolatedUtc { get; set; }
    bool Resolved { get; set; }
    DateTime? ResolvedUtc { get; set; }
    string? Notes { get; set; }
}

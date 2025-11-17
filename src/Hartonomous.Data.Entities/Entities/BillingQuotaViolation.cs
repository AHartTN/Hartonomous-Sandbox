using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class BillingQuotaViolation : IBillingQuotaViolation
{
    public long ViolationId { get; set; }

    public int TenantId { get; set; }

    public string UsageType { get; set; } = null!;

    public long QuotaLimit { get; set; }

    public long CurrentUsage { get; set; }

    public DateTime ViolatedUtc { get; set; }

    public bool Resolved { get; set; }

    public DateTime? ResolvedUtc { get; set; }

    public string? Notes { get; set; }
}

using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class BillingTenantQuota : IBillingTenantQuota
{
    public int QuotaId { get; set; }

    public int TenantId { get; set; }

    public string UsageType { get; set; } = null!;

    public long QuotaLimit { get; set; }

    public bool IsActive { get; set; }

    public string? ResetInterval { get; set; }

    public string? Description { get; set; }

    public string? MetadataJson { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }
}

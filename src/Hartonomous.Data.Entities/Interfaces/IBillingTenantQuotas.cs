using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IBillingTenantQuotas
{
    int QuotaId { get; set; }
    int TenantId { get; set; }
    string UsageType { get; set; }
    long QuotaLimit { get; set; }
    bool IsActive { get; set; }
    string? ResetInterval { get; set; }
    string? Description { get; set; }
    string? MetadataJson { get; set; }
    DateTime CreatedUtc { get; set; }
    DateTime UpdatedUtc { get; set; }
}

using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class TenantSecurityPolicy : ITenantSecurityPolicy
{
    public int PolicyId { get; set; }

    public string TenantId { get; set; } = null!;

    public string PolicyName { get; set; } = null!;

    public string PolicyType { get; set; } = null!;

    public string PolicyRules { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? UpdatedUtc { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }
}

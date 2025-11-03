using System;

namespace Hartonomous.Core.Entities;

public sealed class BillingMultiplier
{
    public Guid MultiplierId { get; set; }

    public Guid RatePlanId { get; set; }

    public string Dimension { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public decimal Multiplier { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public BillingRatePlan RatePlan { get; set; } = null!;
}

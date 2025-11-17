using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class BillingMultipliers : IBillingMultipliers
{
    public Guid MultiplierId { get; set; }

    public Guid RatePlanId { get; set; }

    public string Dimension { get; set; } = null!;

    public string Key { get; set; } = null!;

    public decimal Multiplier { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public virtual BillingRatePlans RatePlan { get; set; } = null!;
}

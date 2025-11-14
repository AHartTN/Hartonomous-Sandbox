using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class BillingPricingTier : IBillingPricingTier
{
    public int TierId { get; set; }

    public string UsageType { get; set; } = null!;

    public string UnitType { get; set; } = null!;

    public decimal UnitPrice { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public string? Description { get; set; }

    public string? MetadataJson { get; set; }

    public DateTime CreatedUtc { get; set; }
}

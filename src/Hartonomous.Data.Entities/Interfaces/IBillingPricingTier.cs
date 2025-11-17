using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface IBillingPricingTier
{
    int TierId { get; set; }
    string UsageType { get; set; }
    string UnitType { get; set; }
    decimal UnitPrice { get; set; }
    DateTime EffectiveFrom { get; set; }
    DateTime? EffectiveTo { get; set; }
    string? Description { get; set; }
    string? MetadataJson { get; set; }
    DateTime CreatedUtc { get; set; }
}

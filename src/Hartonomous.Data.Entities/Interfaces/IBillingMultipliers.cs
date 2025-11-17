using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IBillingMultipliers
{
    Guid MultiplierId { get; set; }
    Guid RatePlanId { get; set; }
    string Dimension { get; set; }
    string Key { get; set; }
    decimal Multiplier { get; set; }
    bool IsActive { get; set; }
    DateTime CreatedUtc { get; set; }
    DateTime UpdatedUtc { get; set; }
    BillingRatePlans RatePlan { get; set; }
}

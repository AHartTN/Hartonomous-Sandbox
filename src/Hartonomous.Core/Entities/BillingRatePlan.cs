using System;
using System.Collections.Generic;

namespace Hartonomous.Core.Entities;

public sealed class BillingRatePlan
{
    public Guid RatePlanId { get; set; }

    public string? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal DefaultRate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public ICollection<BillingOperationRate> OperationRates { get; } = new List<BillingOperationRate>();

    public ICollection<BillingMultiplier> Multipliers { get; } = new List<BillingMultiplier>();
}

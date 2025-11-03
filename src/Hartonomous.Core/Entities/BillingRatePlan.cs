using System;
using System.Collections.Generic;

namespace Hartonomous.Core.Entities;

public sealed class BillingRatePlan
{
    public Guid RatePlanId { get; set; }

    public string? TenantId { get; set; }

    public string PlanCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal DefaultRate { get; set; }

    public decimal MonthlyFee { get; set; }

    public decimal UnitPricePerDcu { get; set; } = 0.00008m;

    public decimal IncludedPublicStorageGb { get; set; }

    public decimal IncludedPrivateStorageGb { get; set; }

    public int IncludedSeatCount { get; set; } = 1;

    public bool AllowsPrivateData { get; set; }

    public bool CanQueryPublicCorpus { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public ICollection<BillingOperationRate> OperationRates { get; } = new List<BillingOperationRate>();

    public ICollection<BillingMultiplier> Multipliers { get; } = new List<BillingMultiplier>();
}

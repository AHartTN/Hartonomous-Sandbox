using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class BillingRatePlan : IBillingRatePlan
{
    public Guid RatePlanId { get; set; }

    public string? TenantId { get; set; }

    public string PlanCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal DefaultRate { get; set; }

    public decimal MonthlyFee { get; set; }

    public decimal UnitPricePerDcu { get; set; }

    public decimal IncludedPublicStorageGb { get; set; }

    public decimal IncludedPrivateStorageGb { get; set; }

    public int IncludedSeatCount { get; set; }

    public bool AllowsPrivateData { get; set; }

    public bool CanQueryPublicCorpus { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public virtual ICollection<BillingMultiplier> BillingMultiplier { get; set; } = new List<BillingMultiplier>();

    public virtual ICollection<BillingOperationRate> BillingOperationRate { get; set; } = new List<BillingOperationRate>();
}

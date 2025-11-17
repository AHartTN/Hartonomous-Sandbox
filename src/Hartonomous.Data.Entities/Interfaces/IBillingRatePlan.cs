using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IBillingRatePlan
{
    Guid RatePlanId { get; set; }
    string? TenantId { get; set; }
    string PlanCode { get; set; }
    string Name { get; set; }
    string? Description { get; set; }
    decimal DefaultRate { get; set; }
    decimal MonthlyFee { get; set; }
    decimal UnitPricePerDcu { get; set; }
    decimal IncludedPublicStorageGb { get; set; }
    decimal IncludedPrivateStorageGb { get; set; }
    int IncludedSeatCount { get; set; }
    bool AllowsPrivateData { get; set; }
    bool CanQueryPublicCorpus { get; set; }
    bool IsActive { get; set; }
    DateTime CreatedUtc { get; set; }
    DateTime UpdatedUtc { get; set; }
    ICollection<BillingMultiplier> BillingMultiplier { get; set; }
    ICollection<BillingOperationRate> BillingOperationRate { get; set; }
}

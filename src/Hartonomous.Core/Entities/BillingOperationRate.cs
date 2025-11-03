using System;

namespace Hartonomous.Core.Entities;

public sealed class BillingOperationRate
{
    public Guid OperationRateId { get; set; }

    public Guid RatePlanId { get; set; }

    public string Operation { get; set; } = string.Empty;

    public decimal Rate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public BillingRatePlan RatePlan { get; set; } = null!;
}

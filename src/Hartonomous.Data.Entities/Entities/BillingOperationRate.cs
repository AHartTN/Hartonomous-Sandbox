using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public partial class BillingOperationRate : IBillingOperationRate
{
    public Guid OperationRateId { get; set; }

    public Guid RatePlanId { get; set; }

    public string Operation { get; set; } = null!;

    public string UnitOfMeasure { get; set; } = null!;

    public string? Category { get; set; }

    public string? Description { get; set; }

    public decimal Rate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public virtual BillingRatePlan RatePlan { get; set; } = null!;
}

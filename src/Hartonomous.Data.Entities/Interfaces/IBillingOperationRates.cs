using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IBillingOperationRates
{
    Guid OperationRateId { get; set; }
    Guid RatePlanId { get; set; }
    string Operation { get; set; }
    string UnitOfMeasure { get; set; }
    string? Category { get; set; }
    string? Description { get; set; }
    decimal Rate { get; set; }
    bool IsActive { get; set; }
    DateTime CreatedUtc { get; set; }
    DateTime UpdatedUtc { get; set; }
    BillingRatePlans RatePlan { get; set; }
}

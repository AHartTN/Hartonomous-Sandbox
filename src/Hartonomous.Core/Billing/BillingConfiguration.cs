using System;
using System.Collections.Generic;

namespace Hartonomous.Core.Billing;

public sealed class BillingConfiguration
{
    public Guid? RatePlanId { get; init; }

    public decimal DefaultRate { get; init; }

    public IReadOnlyDictionary<string, decimal> OperationRates { get; init; }
        = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, decimal> GenerationTypeMultipliers { get; init; }
        = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, decimal> ComplexityMultipliers { get; init; }
        = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, decimal> ContentTypeMultipliers { get; init; }
        = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

    public static BillingConfiguration Empty { get; } = new()
    {
        DefaultRate = 0m
    };
}

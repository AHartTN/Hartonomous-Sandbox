using System;
using System.Collections.Generic;

namespace Hartonomous.Core.Configuration;

public sealed class BillingOptions
{
    public const string SectionName = "Billing";

    public decimal DefaultRate { get; set; } = 0.01m;

    public Dictionary<string, decimal> OperationRates { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, decimal> GenerationTypeMultipliers { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, decimal> ComplexityMultipliers { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, decimal> ContentTypeMultipliers { get; } = new(StringComparer.OrdinalIgnoreCase);
}

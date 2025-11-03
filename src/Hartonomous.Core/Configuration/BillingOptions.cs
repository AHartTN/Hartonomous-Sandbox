using System;
using System.Collections.Generic;

namespace Hartonomous.Core.Configuration;

public sealed class BillingOptions
{
    public const string SectionName = "Billing";

    public decimal DefaultRate { get; set; } = 0.01m;

    public decimal UnitPricePerDcu { get; set; } = 0.00008m;

    public string DefaultPlanName { get; set; } = "Default";

    public string DefaultPlanCode { get; set; } = "default";

    public decimal DefaultMonthlyFee { get; set; }
        = 0m;

    public decimal DefaultIncludedPublicStorageGb { get; set; }
        = 0m;

    public decimal DefaultIncludedPrivateStorageGb { get; set; }
        = 0m;

    public int DefaultIncludedSeatCount { get; set; }
        = 1;

    public bool DefaultAllowsPrivateData { get; set; }
        = false;

    public bool DefaultCanQueryPublicCorpus { get; set; }
        = false;

    public Dictionary<string, decimal> OperationRates { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> OperationUnits { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string?> OperationCategories { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, decimal> GenerationTypeMultipliers { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, decimal> ComplexityMultipliers { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, decimal> ContentTypeMultipliers { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, decimal> GroundingMultipliers { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, decimal> GuaranteeMultipliers { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, decimal> ProvenanceMultipliers { get; } = new(StringComparer.OrdinalIgnoreCase);
}

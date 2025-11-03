using System;
using System.Collections.Generic;

namespace Hartonomous.Core.Billing;

public sealed class BillingConfiguration
{
    public Guid? RatePlanId { get; init; }

    public string? PlanName { get; init; }

    public string PlanCode { get; init; } = string.Empty;

    public decimal DefaultRate { get; init; }

    public decimal MonthlyFee { get; init; }

    public decimal UnitPricePerDcu { get; init; } = 0.00008m;

    public decimal IncludedPublicStorageGb { get; init; }

    public decimal IncludedPrivateStorageGb { get; init; }

    public int IncludedSeatCount { get; init; } = 1;

    public bool AllowsPrivateData { get; init; }

    public bool CanQueryPublicCorpus { get; init; }

    public IReadOnlyDictionary<string, decimal> OperationRates { get; init; }
        = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> OperationUnits { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string?> OperationCategories { get; init; }
        = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, decimal> GenerationTypeMultipliers { get; init; }
        = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, decimal> ComplexityMultipliers { get; init; }
        = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, decimal> ContentTypeMultipliers { get; init; }
        = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, decimal> GroundingMultipliers { get; init; }
        = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, decimal> GuaranteeMultipliers { get; init; }
        = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, decimal> ProvenanceMultipliers { get; init; }
        = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

    public static BillingConfiguration Empty { get; } = new()
    {
        DefaultRate = 0m,
        UnitPricePerDcu = 0.00008m,
        PlanCode = string.Empty,
        MonthlyFee = 0m,
        IncludedPublicStorageGb = 0m,
        IncludedPrivateStorageGb = 0m,
        IncludedSeatCount = 1,
        AllowsPrivateData = false,
        CanQueryPublicCorpus = false
    };
}

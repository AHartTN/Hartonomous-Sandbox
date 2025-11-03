using System;
using System.Collections.Generic;

namespace Hartonomous.Core.Billing;

public sealed class BillingUsageRecord
{
    public string TenantId { get; init; } = string.Empty;

    public string PrincipalId { get; init; } = string.Empty;

    public string Operation { get; init; } = string.Empty;

    public string MessageType { get; init; } = string.Empty;

    public string Handler { get; init; } = string.Empty;

    public decimal Units { get; init; }
        = 1m;

    public decimal BaseRate { get; init; }
        = 0m;

    public decimal Multiplier { get; init; }
        = 1m;

    public decimal TotalCost => Math.Round(Units * BaseRate * Multiplier, 6, MidpointRounding.AwayFromZero);

    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();

    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
}

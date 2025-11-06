using System;
using System.Collections.Generic;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a billing rate plan defining pricing structure for tenant services.
/// Rate plans include base fees, operation-specific rates, and multipliers for dynamic pricing.
/// </summary>
public sealed class BillingRatePlan
{
    /// <summary>
    /// Gets or sets the unique identifier for the rate plan.
    /// </summary>
    public Guid RatePlanId { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier this rate plan applies to (null for default/public plans).
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the unique code identifying this plan (e.g., 'FREE', 'BASIC', 'PRO', 'ENTERPRISE').
    /// </summary>
    public string PlanCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable name of the rate plan.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a description of the rate plan's features and benefits.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the default rate applied when no specific operation rate is defined.
    /// </summary>
    public decimal DefaultRate { get; set; }

    /// <summary>
    /// Gets or sets the fixed monthly subscription fee for this plan.
    /// </summary>
    public decimal MonthlyFee { get; set; }

    /// <summary>
    /// Gets or sets the price per Database Compute Unit (DCU) for usage-based billing.
    /// Default value is $0.00008 per DCU.
    /// </summary>
    public decimal UnitPricePerDcu { get; set; } = 0.00008m;

    /// <summary>
    /// Gets or sets the amount of included public corpus storage in gigabytes.
    /// </summary>
    public decimal IncludedPublicStorageGb { get; set; }

    /// <summary>
    /// Gets or sets the amount of included private tenant storage in gigabytes.
    /// </summary>
    public decimal IncludedPrivateStorageGb { get; set; }

    /// <summary>
    /// Gets or sets the number of included user seats/licenses. Default is 1.
    /// </summary>
    public int IncludedSeatCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets a value indicating whether this plan allows private tenant data storage and processing.
    /// </summary>
    public bool AllowsPrivateData { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this plan can query the shared public corpus.
    /// </summary>
    public bool CanQueryPublicCorpus { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this rate plan is currently active and available.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the timestamp when the rate plan was created (UTC).
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the rate plan was last updated (UTC).
    /// </summary>
    public DateTime UpdatedUtc { get; set; }

    /// <summary>
    /// Gets the collection of operation-specific rates for this plan.
    /// </summary>
    public ICollection<BillingOperationRate> OperationRates { get; } = new List<BillingOperationRate>();

    /// <summary>
    /// Gets the collection of rate multipliers for dynamic pricing based on dimensions.
    /// </summary>
    public ICollection<BillingMultiplier> Multipliers { get; } = new List<BillingMultiplier>();
}

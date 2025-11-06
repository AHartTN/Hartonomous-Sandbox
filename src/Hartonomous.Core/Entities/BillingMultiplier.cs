using System;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a billing rate multiplier that adjusts prices based on specific dimensions or conditions.
/// Enables dynamic pricing based on factors like time of day, data region, model type, or priority levels.
/// </summary>
public sealed class BillingMultiplier
{
    /// <summary>
    /// Gets or sets the unique identifier for the multiplier.
    /// </summary>
    public Guid MultiplierId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the parent rate plan.
    /// </summary>
    public Guid RatePlanId { get; set; }

    /// <summary>
    /// Gets or sets the dimension category for this multiplier (e.g., 'time', 'region', 'model_type', 'priority').
    /// </summary>
    public string Dimension { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the specific key value within the dimension (e.g., 'peak_hours', 'us-east', 'gpt-4', 'high_priority').
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the multiplier value applied to base rates (e.g., 1.5 for 50% surcharge, 0.8 for 20% discount).
    /// </summary>
    public decimal Multiplier { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this multiplier is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the timestamp when the multiplier was created (UTC).
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the multiplier was last updated (UTC).
    /// </summary>
    public DateTime UpdatedUtc { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent rate plan.
    /// </summary>
    public BillingRatePlan RatePlan { get; set; } = null!;
}

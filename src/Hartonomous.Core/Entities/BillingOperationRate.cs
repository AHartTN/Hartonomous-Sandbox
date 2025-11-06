using System;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a billing rate for a specific operation within a rate plan.
/// Enables granular pricing for different types of operations (e.g., inference, embedding, storage).
/// </summary>
public sealed class BillingOperationRate
{
    /// <summary>
    /// Gets or sets the unique identifier for the operation rate.
    /// </summary>
    public Guid OperationRateId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the parent rate plan.
    /// </summary>
    public Guid RatePlanId { get; set; }

    /// <summary>
    /// Gets or sets the name of the operation being billed (e.g., 'inference', 'embedding', 'vector_search', 'storage').
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unit of measure for billing this operation (e.g., 'per_request', 'per_token', 'per_gb', 'per_hour').
    /// </summary>
    public string UnitOfMeasure { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category of the operation (e.g., 'compute', 'storage', 'network', 'ai_services').
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets a description of what this operation rate covers.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the billing rate for this operation in the plan's currency.
    /// </summary>
    public decimal Rate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this operation rate is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the timestamp when the operation rate was created (UTC).
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the operation rate was last updated (UTC).
    /// </summary>
    public DateTime UpdatedUtc { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent rate plan.
    /// </summary>
    public BillingRatePlan RatePlan { get; set; } = null!;
}

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents deduplication configuration values for ingestion pipelines.
/// Policies define thresholds for semantic and spatial similarity used to determine if content is duplicate.
/// </summary>
public class DeduplicationPolicy
{
    /// <summary>
    /// Gets or sets the unique identifier for the deduplication policy.
    /// </summary>
    public int DeduplicationPolicyId { get; set; }

    /// <summary>
    /// Gets or sets the human-readable name of the policy.
    /// </summary>
    public required string PolicyName { get; set; }

    /// <summary>
    /// Gets or sets the cosine similarity threshold for semantic deduplication (0.0 to 1.0).
    /// Embeddings with similarity above this threshold are considered duplicates.
    /// </summary>
    public double? SemanticThreshold { get; set; }

    /// <summary>
    /// Gets or sets the spatial distance threshold for geometric deduplication.
    /// Points within this distance in spatial space are considered duplicates.
    /// </summary>
    public double? SpatialThreshold { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON (e.g., hash algorithms, modality-specific rules).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this policy is currently active and in use.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the timestamp when the policy was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

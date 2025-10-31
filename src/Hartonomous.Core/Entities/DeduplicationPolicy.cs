namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents deduplication configuration values for ingestion pipelines.
/// </summary>
public class DeduplicationPolicy
{
    public int DeduplicationPolicyId { get; set; }

    public required string PolicyName { get; set; }

    public double? SemanticThreshold { get; set; }

    public double? SpatialThreshold { get; set; }

    public string? Metadata { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Provenance;

/// <summary>
/// Request model for error clusters queries with validation.
/// </summary>
public class ErrorClustersRequest
{
    /// <summary>
    /// Optional: Filter to specific session.
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "Session ID must be greater than 0")]
    public long? SessionId { get; set; }

    /// <summary>
    /// Minimum errors per cluster (default: 3).
    /// </summary>
    [Range(1, 100, ErrorMessage = "Minimum cluster size must be between 1 and 100")]
    public int MinClusterSize { get; set; } = 3;
}
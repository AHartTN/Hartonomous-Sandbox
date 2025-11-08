using Hartonomous.Core.Entities;
using Hartonomous.Core.Shared;

namespace Hartonomous.Infrastructure.Repositories.EfCore.Models;

/// <summary>
/// Result of the learning phase
/// </summary>
public class LearningResult
{
    /// <summary>
    /// Unique ID for this learning session
    /// </summary>
    public Guid LearningId { get; set; }

    /// <summary>
    /// Insights gained from performance analysis
    /// </summary>
    public IReadOnlyList<string> Insights { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Recommendations for system improvement
    /// </summary>
    public IReadOnlyList<string> Recommendations { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Confidence score for the learning (0-1)
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Whether the learning indicates system health
    /// </summary>
    public bool IsSystemHealthy { get; set; }

    /// <summary>
    /// Timestamp of learning completion
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Configuration for the next OODA loop cycle
/// </summary>

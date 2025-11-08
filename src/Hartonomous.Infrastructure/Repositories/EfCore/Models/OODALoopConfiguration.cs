using Hartonomous.Core.Entities;
using Hartonomous.Core.Shared;

namespace Hartonomous.Infrastructure.Repositories.EfCore.Models;

/// <summary>
/// Configuration for the next OODA loop cycle
/// </summary>
public class OODALoopConfiguration
{
    /// <summary>
    /// Analysis interval in minutes
    /// </summary>
    public int AnalysisIntervalMinutes { get; set; } = 15;

    /// <summary>
    /// Action approval threshold
    /// </summary>
    public int AutoApproveThreshold { get; set; } = 3;

    /// <summary>
    /// Whether to enable aggressive optimization
    /// </summary>
    public bool EnableAggressiveOptimization { get; set; }

    /// <summary>
    /// Priority weights for different hypothesis types
    /// </summary>
    public IReadOnlyDictionary<string, int> HypothesisPriorityWeights { get; set; } = new Dictionary<string, int>();

    /// <summary>
    /// Timestamp of configuration creation
    /// </summary>
    public DateTime Timestamp { get; set; }
}

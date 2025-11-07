using Hartonomous.Core.Entities;
using Hartonomous.Core.Shared;

namespace Hartonomous.Data.Repositories;

/// <summary>
/// Interface for autonomous learning operations
/// Replaces sp_Learn stored procedure functionality
/// </summary>
public interface IAutonomousLearningRepository
{
    /// <summary>
    /// Learns from system performance and stores improvement history
    /// </summary>
    /// <param name="performanceMetrics">Current system performance metrics</param>
    /// <param name="improvementActions">Actions that were taken</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Learning result with insights and recommendations</returns>
    Task<LearningResult> LearnFromPerformanceAsync(
        PerformanceMetrics performanceMetrics,
        IReadOnlyList<ActionResult> improvementActions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restarts the OODA loop with learned insights
    /// </summary>
    /// <param name="learningResult">Results from the learning phase</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Next cycle configuration</returns>
    Task<OODALoopConfiguration> RestartOODALoopAsync(
        LearningResult learningResult,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Performance metrics for learning
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Throughput (requests per second)
    /// </summary>
    public double Throughput { get; set; }

    /// <summary>
    /// Error rate (percentage)
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Memory usage percentage
    /// </summary>
    public double MemoryUsagePercent { get; set; }

    /// <summary>
    /// CPU usage percentage
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// Database query performance score
    /// </summary>
    public double DatabasePerformanceScore { get; set; }

    /// <summary>
    /// Timestamp of metrics collection
    /// </summary>
    public DateTime Timestamp { get; set; }
}

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
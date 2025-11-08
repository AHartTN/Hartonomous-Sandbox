using Hartonomous.Core.Entities;
using Hartonomous.Core.Shared;

namespace Hartonomous.Infrastructure.Repositories.EfCore.Models;

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

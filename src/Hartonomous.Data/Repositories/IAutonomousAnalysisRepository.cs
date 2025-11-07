using Hartonomous.Core.Entities;
using Hartonomous.Core.Shared;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Repositories;

/// <summary>
/// Interface for autonomous analysis operations
/// Replaces sp_Analyze stored procedure
/// </summary>
public interface IAutonomousAnalysisRepository
{
    /// <summary>
    /// Performs system observation and analysis to detect anomalies and patterns
    /// </summary>
    Task<AnalysisResult> AnalyzeSystemAsync(
        int tenantId = 0,
        string analysisScope = "full",
        int lookbackHours = 24,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a system analysis operation
/// </summary>
public class AnalysisResult
{
    public Guid AnalysisId { get; set; }
    public string Scope { get; set; } = string.Empty;
    public int LookbackHours { get; set; }
    public int TotalInferences { get; set; }
    public double AvgDurationMs { get; set; }
    public int AnomalyCount { get; set; }
    public IReadOnlyList<PerformanceAnomaly> Anomalies { get; set; } = Array.Empty<PerformanceAnomaly>();
    public IReadOnlyList<EmbeddingPattern> Patterns { get; set; } = Array.Empty<EmbeddingPattern>();
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Represents a performance anomaly detected during analysis
/// </summary>
public class PerformanceAnomaly
{
    public long InferenceRequestId { get; set; }
    public int? ModelId { get; set; }
    public int DurationMs { get; set; }
    public double AvgDurationMs { get; set; }
    public double SlowdownFactor { get; set; }
}

/// <summary>
/// Represents an embedding pattern detected during analysis
/// </summary>
public class EmbeddingPattern
{
    public long AtomId { get; set; }
    public string Modality { get; set; } = string.Empty;
    public int ClusterSize { get; set; }
}
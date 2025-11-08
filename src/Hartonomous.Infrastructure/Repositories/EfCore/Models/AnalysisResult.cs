using Hartonomous.Core.Entities;
using Hartonomous.Core.Shared;

namespace Hartonomous.Infrastructure.Repositories.EfCore.Models;

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

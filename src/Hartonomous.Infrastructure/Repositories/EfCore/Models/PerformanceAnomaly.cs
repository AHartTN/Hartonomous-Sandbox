namespace Hartonomous.Infrastructure.Repositories.EfCore.Models;

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

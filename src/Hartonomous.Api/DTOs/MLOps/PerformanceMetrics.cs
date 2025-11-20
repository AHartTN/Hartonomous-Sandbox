namespace Hartonomous.Api.DTOs.MLOps;

public class PerformanceMetrics
{
    public int AverageLatencyMs { get; set; }
    public int P50LatencyMs { get; set; }
    public int P95LatencyMs { get; set; }
    public int P99LatencyMs { get; set; }
    public int RequestsPerSecond { get; set; }
    public double ErrorRate { get; set; }
    public double SuccessRate { get; set; }
}

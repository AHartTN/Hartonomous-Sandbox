namespace Hartonomous.Api.DTOs.Operations;

/// <summary>
/// System-level metrics for Prometheus/Grafana
/// </summary>
public class SystemMetricsResponse
{
    public double CpuPercent { get; set; }
    public double MemoryPercent { get; set; }
    public double StoragePercent { get; set; }
    public required Dictionary<string, int> QueueDepths { get; set; }
    public int ActiveInferences { get; set; }
    public long TotalAtoms { get; set; }
    public long TotalEmbeddings { get; set; }
    public DateTime MeasuredAt { get; set; }
}

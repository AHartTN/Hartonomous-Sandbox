namespace Hartonomous.Api.DTOs.MLOps;

public class ModelMetrics
{
    public long Requests { get; set; }
    public int AverageLatency { get; set; }
    public double Accuracy { get; set; }
    public double CacheHitRate { get; set; }
}

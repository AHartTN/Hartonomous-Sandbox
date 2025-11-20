namespace Hartonomous.Api.DTOs.MLOps;

public class ModelStatistics
{
    public int TotalModels { get; set; }
    public int ProductionModels { get; set; }
    public int CanaryModels { get; set; }
    public int TestingModels { get; set; }
    public double AverageAccuracy { get; set; }
    public double AverageLatency { get; set; }
    public long TotalInferences { get; set; }
}

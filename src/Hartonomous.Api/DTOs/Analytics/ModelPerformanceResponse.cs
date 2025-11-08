namespace Hartonomous.Api.DTOs.Analytics;

public class ModelPerformanceResponse
{
    public required List<ModelPerformanceMetric> Metrics { get; set; }
}

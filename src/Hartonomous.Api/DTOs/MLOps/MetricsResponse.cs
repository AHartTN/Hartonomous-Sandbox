using System;
using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.MLOps;


public class MetricsResponse
{
    public string TimeWindow { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public PerformanceMetrics Performance { get; set; } = new();
    public Dictionary<string, ModelMetrics> Models { get; set; } = new();
    public ResourceMetrics Resources { get; set; } = new();
    public bool DemoMode { get; set; }
}

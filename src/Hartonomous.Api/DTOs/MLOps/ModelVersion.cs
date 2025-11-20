using System;

namespace Hartonomous.Api.DTOs.MLOps;


public class ModelVersion
{
    public string ModelId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime DeployedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public double Accuracy { get; set; }
    public int LatencyMs { get; set; }
    public int ThroughputQps { get; set; }
    public string TrainingDataSize { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

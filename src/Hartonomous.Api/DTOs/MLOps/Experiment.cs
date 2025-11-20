using System;

namespace Hartonomous.Api.DTOs.MLOps;


public class Experiment
{
    public string ExperimentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public TrafficAllocation Traffic { get; set; } = new();
    public ExperimentMetrics Metrics { get; set; } = new();
    public string Decision { get; set; } = string.Empty;
}

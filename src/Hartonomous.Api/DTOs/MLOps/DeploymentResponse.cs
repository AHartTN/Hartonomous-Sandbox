using System;
using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.MLOps;


public class DeploymentResponse
{
    public string DeploymentId { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<DeploymentStep> Steps { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime? EstimatedCompletion { get; set; }
    public bool RollbackAvailable { get; set; }
    public bool DemoMode { get; set; }
    public string Message { get; set; } = string.Empty;
}

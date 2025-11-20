using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.MLOps;


public class ModelVersionsResponse
{
    public List<ModelVersion> Models { get; set; } = new();
    public ModelStatistics Statistics { get; set; } = new();
    public bool DemoMode { get; set; }
}

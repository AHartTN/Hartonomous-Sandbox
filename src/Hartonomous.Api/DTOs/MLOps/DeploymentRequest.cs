namespace Hartonomous.Api.DTOs.MLOps;

public class DeploymentRequest
{
    public string ModelId { get; set; } = string.Empty;
    public string TargetEnvironment { get; set; } = "production";
    public bool AutoRollback { get; set; } = true;
}

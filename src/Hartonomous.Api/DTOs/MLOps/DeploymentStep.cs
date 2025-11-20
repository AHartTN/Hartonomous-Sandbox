namespace Hartonomous.Api.DTOs.MLOps;

public class DeploymentStep
{
    public string Step { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Duration { get; set; }
    public string Details { get; set; } = string.Empty;
}

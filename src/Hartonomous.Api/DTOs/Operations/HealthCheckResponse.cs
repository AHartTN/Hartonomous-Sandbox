namespace Hartonomous.Api.DTOs.Operations;

public class HealthCheckResponse
{
    public required string Status { get; set; } // healthy, degraded, unhealthy
    public required Dictionary<string, ComponentHealth> Components { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan TotalCheckDuration { get; set; }
}

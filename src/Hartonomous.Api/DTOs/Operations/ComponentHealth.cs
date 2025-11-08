namespace Hartonomous.Api.DTOs.Operations;

public class ComponentHealth
{
    public required string Status { get; set; }
    public string? Message { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}

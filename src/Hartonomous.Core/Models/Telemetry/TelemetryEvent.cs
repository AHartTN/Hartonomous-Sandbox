namespace Hartonomous.Core.Models.Telemetry;

/// <summary>
/// Event/alert from a device.
/// </summary>
public class TelemetryEvent
{
    public required string Message { get; set; }
    public required string Severity { get; set; }
    public string? Code { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

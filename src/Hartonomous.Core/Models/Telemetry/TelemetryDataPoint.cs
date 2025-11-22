namespace Hartonomous.Core.Models.Telemetry;

/// <summary>
/// Represents a single telemetry data point from a device/sensor.
/// </summary>
public class TelemetryDataPoint
{
    public required string DeviceId { get; set; }
    public string? DeviceType { get; set; }
    public string? Location { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public long SequenceNumber { get; set; }
    public required List<TelemetryMetric> Metrics { get; set; }
    public List<TelemetryEvent>? Events { get; set; }
}

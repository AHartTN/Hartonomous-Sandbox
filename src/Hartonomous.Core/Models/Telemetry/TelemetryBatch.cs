namespace Hartonomous.Core.Models.Telemetry;

/// <summary>
/// Batch of telemetry readings from a single sensor.
/// </summary>
public class TelemetryBatch
{
    public required string SensorId { get; set; }
    public required string SensorType { get; set; }
    public required string Unit { get; set; }
    public required List<TelemetryReading> Readings { get; set; }
}

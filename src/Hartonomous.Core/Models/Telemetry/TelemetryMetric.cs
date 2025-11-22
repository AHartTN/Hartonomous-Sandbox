namespace Hartonomous.Core.Models.Telemetry;

/// <summary>
/// Individual measurement/metric from a sensor.
/// </summary>
public class TelemetryMetric
{
    public required string Name { get; set; }
    public required object Value { get; set; }
    public string? Unit { get; set; }
}

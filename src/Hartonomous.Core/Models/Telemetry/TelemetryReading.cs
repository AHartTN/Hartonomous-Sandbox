namespace Hartonomous.Core.Models.Telemetry;

/// <summary>
/// Single telemetry reading with timestamp and value.
/// </summary>
public class TelemetryReading
{
    public required DateTime Timestamp { get; set; }
    public required TelemetryValueType ValueType { get; set; }
    
    public double DoubleValue { get; set; }
    public long IntValue { get; set; }
    public bool BoolValue { get; set; }
    public string? StringValue { get; set; }
    
    public int Quality { get; set; } = 100;
}

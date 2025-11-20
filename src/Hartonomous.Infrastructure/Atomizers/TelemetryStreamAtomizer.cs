using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes streaming telemetry data: sensor readings, SCADA values, metrics.
/// Converts time-series data into individual measurement atoms with temporal positions.
/// </summary>
public class TelemetryStreamAtomizer : IAtomizer<TelemetryBatch>
{
    private const int MaxAtomSize = 64;
    public int Priority => 60;

    public bool CanHandle(string contentType, string? fileExtension)
    {
        return false; // Invoked explicitly via TelemetryBatch
    }

    public async Task<AtomizationResult> AtomizeAsync(
        TelemetryBatch batch,
        SourceMetadata source,
        CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();

        try
        {
            // Create sensor/source atom
            var sensorBytes = Encoding.UTF8.GetBytes(batch.SensorId);
            var sensorHash = SHA256.HashData(sensorBytes);
            var sensorAtom = new AtomData
            {
                AtomicValue = sensorBytes,
                ContentHash = sensorHash,
                Modality = "telemetry",
                Subtype = "sensor-id",
                ContentType = "application/x-telemetry",
                CanonicalText = batch.SensorId,
                Metadata = $"{{\"sensorId\":\"{batch.SensorId}\",\"sensorType\":\"{batch.SensorType}\",\"unit\":\"{batch.Unit}\"}}"
            };
            atoms.Add(sensorAtom);

            int measurementIndex = 0;
            foreach (var reading in batch.Readings)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Create timestamp atom
                var timestampBytes = BitConverter.GetBytes(reading.Timestamp.Ticks);
                var timestampHash = SHA256.HashData(timestampBytes);
                
                if (!atoms.Any(a => a.ContentHash.SequenceEqual(timestampHash)))
                {
                    var timestampAtom = new AtomData
                    {
                        AtomicValue = timestampBytes,
                        ContentHash = timestampHash,
                        Modality = "telemetry",
                        Subtype = "timestamp",
                        ContentType = "application/x-telemetry",
                        CanonicalText = reading.Timestamp.ToString("O"),
                        Metadata = $"{{\"ticks\":{reading.Timestamp.Ticks},\"iso\":\"{reading.Timestamp:O}\"}}"
                    };
                    atoms.Add(timestampAtom);
                }

                // Create value atom (actual measurement)
                byte[] valueBytes;
                string valueCanonical;
                
                switch (reading.ValueType)
                {
                    case TelemetryValueType.Double:
                        valueBytes = BitConverter.GetBytes(reading.DoubleValue);
                        valueCanonical = reading.DoubleValue.ToString("G17");
                        break;
                    case TelemetryValueType.Integer:
                        valueBytes = BitConverter.GetBytes(reading.IntValue);
                        valueCanonical = reading.IntValue.ToString();
                        break;
                    case TelemetryValueType.Boolean:
                        valueBytes = BitConverter.GetBytes(reading.BoolValue);
                        valueCanonical = reading.BoolValue.ToString().ToLower();
                        break;
                    case TelemetryValueType.String:
                        valueBytes = Encoding.UTF8.GetBytes(reading.StringValue ?? "");
                        if (valueBytes.Length > MaxAtomSize)
                            valueBytes = valueBytes.Take(MaxAtomSize).ToArray();
                        valueCanonical = reading.StringValue ?? "";
                        break;
                    default:
                        warnings.Add($"Unknown value type: {reading.ValueType}");
                        continue;
                }

                var valueHash = SHA256.HashData(valueBytes);
                
                if (!atoms.Any(a => a.ContentHash.SequenceEqual(valueHash)))
                {
                    var valueAtom = new AtomData
                    {
                        AtomicValue = valueBytes,
                        ContentHash = valueHash,
                        Modality = "telemetry",
                        Subtype = $"value-{reading.ValueType.ToString().ToLowerInvariant()}",
                        ContentType = "application/x-telemetry",
                        CanonicalText = valueCanonical,
                        Metadata = $"{{\"type\":\"{reading.ValueType}\",\"unit\":\"{batch.Unit}\",\"quality\":{reading.Quality}}}"
                    };
                    atoms.Add(valueAtom);
                }

                // Create measurement composition (sensor + timestamp + value)
                var measurementBytes = Encoding.UTF8.GetBytes($"{batch.SensorId}:{reading.Timestamp.Ticks}");
                var measurementHash = SHA256.HashData(measurementBytes);
                var measurementAtom = new AtomData
                {
                    AtomicValue = measurementBytes,
                    ContentHash = measurementHash,
                    Modality = "telemetry",
                    Subtype = "measurement",
                    ContentType = "application/x-telemetry",
                    CanonicalText = $"{batch.SensorId}@{reading.Timestamp:O}={valueCanonical}",
                    Metadata = $"{{\"sensorId\":\"{batch.SensorId}\",\"timestamp\":\"{reading.Timestamp:O}\",\"value\":\"{valueCanonical}\"}}"
                };
                atoms.Add(measurementAtom);

                // Link sensor → measurement
                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = sensorHash,
                    ComponentAtomHash = measurementHash,
                    SequenceIndex = measurementIndex,
                    Position = new SpatialPosition 
                    { 
                        X = 0, 
                        Y = measurementIndex, 
                        Z = 0,
                        M = reading.Timestamp.Ticks / 10000000.0 // Seconds as M coordinate
                    }
                });

                // Link measurement → timestamp
                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = measurementHash,
                    ComponentAtomHash = timestampHash,
                    SequenceIndex = 0,
                    Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                });

                // Link measurement → value
                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = measurementHash,
                    ComponentAtomHash = valueHash,
                    SequenceIndex = 1,
                    Position = new SpatialPosition { X = 1, Y = 0, Z = 0 }
                });

                measurementIndex++;
            }

            sw.Stop();

            return new AtomizationResult
            {
                Atoms = atoms,
                Compositions = compositions,
                ProcessingInfo = new ProcessingMetadata
                {
                    TotalAtoms = atoms.Count,
                    UniqueAtoms = atoms.Select(a => Convert.ToBase64String(a.ContentHash)).Distinct().Count(),
                    DurationMs = sw.ElapsedMilliseconds,
                    AtomizerType = nameof(TelemetryStreamAtomizer),
                    DetectedFormat = $"Telemetry - {batch.SensorType} ({batch.Readings.Count} readings)",
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            };
        }
        catch (Exception ex)
        {
            warnings.Add($"Telemetry atomization failed: {ex.Message}");
            throw;
        }
    }
}

/// <summary>
/// Batch of telemetry readings from a single sensor.
/// </summary>
public class TelemetryBatch
{
    public required string SensorId { get; set; }
    public required string SensorType { get; set; } // temperature, pressure, voltage, flow, etc.
    public required string Unit { get; set; } // celsius, psi, volts, m3/h, etc.
    public required List<TelemetryReading> Readings { get; set; }
}

/// <summary>
/// Single telemetry reading with timestamp and value.
/// </summary>
public class TelemetryReading
{
    public required DateTime Timestamp { get; set; }
    public required TelemetryValueType ValueType { get; set; }
    
    // Value variants
    public double DoubleValue { get; set; }
    public long IntValue { get; set; }
    public bool BoolValue { get; set; }
    public string? StringValue { get; set; }
    
    // Quality indicator (0-100, 100 = perfect)
    public int Quality { get; set; } = 100;
}

public enum TelemetryValueType
{
    Double,
    Integer,
    Boolean,
    String
}

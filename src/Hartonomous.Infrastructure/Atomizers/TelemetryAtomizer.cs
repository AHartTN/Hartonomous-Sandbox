using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Models.Telemetry;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes telemetry data streams from IoT devices, sensors, SCADA systems, etc.
/// Supports time-series data with temporal positioning and real-time ingestion.
/// </summary>
public class TelemetryAtomizer : IAtomizer<TelemetryDataPoint>
{
    private const int MaxAtomSize = 64;
    public int Priority => 60;

    public bool CanHandle(string contentType, string? fileExtension)
    {
        return false; // Invoked explicitly via TelemetryDataPoint
    }

    public async Task<AtomizationResult> AtomizeAsync(
        TelemetryDataPoint dataPoint,
        SourceMetadata source,
        CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();

        try
        {
            // Create device/sensor atom
            var deviceIdBytes = Encoding.UTF8.GetBytes(dataPoint.DeviceId);
            var deviceHash = SHA256.HashData(deviceIdBytes);
            var deviceAtom = new AtomData
            {
                AtomicValue = deviceIdBytes.Length <= MaxAtomSize ? deviceIdBytes : deviceIdBytes.Take(MaxAtomSize).ToArray(),
                ContentHash = deviceHash,
                Modality = "telemetry",
                Subtype = dataPoint.DeviceType ?? "sensor",
                ContentType = "application/json",
                CanonicalText = dataPoint.DeviceId,
                Metadata = $"{{\"deviceId\":\"{dataPoint.DeviceId}\",\"deviceType\":\"{dataPoint.DeviceType}\",\"location\":\"{dataPoint.Location}\"}}"
            };
            atoms.Add(deviceAtom);

            // Create measurement atom for each metric
            int metricIndex = 0;
            foreach (var metric in dataPoint.Metrics)
            {
                // Serialize metric value to bytes
                byte[] valueBytes;
                string valueType;
                
                if (metric.Value is double dblVal)
                {
                    valueBytes = BitConverter.GetBytes(dblVal);
                    valueType = "double";
                }
                else if (metric.Value is float fltVal)
                {
                    valueBytes = BitConverter.GetBytes(fltVal);
                    valueType = "float";
                }
                else if (metric.Value is int intVal)
                {
                    valueBytes = BitConverter.GetBytes(intVal);
                    valueType = "int32";
                }
                else if (metric.Value is long lngVal)
                {
                    valueBytes = BitConverter.GetBytes(lngVal);
                    valueType = "int64";
                }
                else if (metric.Value is bool boolVal)
                {
                    valueBytes = BitConverter.GetBytes(boolVal);
                    valueType = "boolean";
                }
                else
                {
                    // String or other - convert to UTF-8
                    var strVal = metric.Value?.ToString() ?? "";
                    valueBytes = Encoding.UTF8.GetBytes(strVal);
                    if (valueBytes.Length > MaxAtomSize)
                        valueBytes = valueBytes.Take(MaxAtomSize).ToArray();
                    valueType = "string";
                }

                var metricHash = SHA256.HashData(valueBytes);
                var metricAtom = new AtomData
                {
                    AtomicValue = valueBytes,
                    ContentHash = metricHash,
                    Modality = "telemetry",
                    Subtype = $"metric-{valueType}",
                    ContentType = "application/octet-stream",
                    CanonicalText = $"{metric.Name}={metric.Value}{metric.Unit}",
                    Metadata = $"{{\"name\":\"{metric.Name}\",\"value\":{JsonSerializer.Serialize(metric.Value)},\"unit\":\"{metric.Unit}\",\"type\":\"{valueType}\"}}"
                };

                // Check if metric value already exists (deduplication)
                if (!atoms.Any(a => a.ContentHash.SequenceEqual(metricHash)))
                {
                    atoms.Add(metricAtom);
                }

                // Link metric to device with temporal position
                // X = metric index, Y = sequence number, Z = 0, M = timestamp
                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = deviceHash,
                    ComponentAtomHash = metricHash,
                    SequenceIndex = dataPoint.SequenceNumber,
                    Position = new SpatialPosition
                    {
                        X = metricIndex,
                        Y = dataPoint.SequenceNumber,
                        Z = 0,
                        M = dataPoint.Timestamp.ToUnixTimeMilliseconds()
                    }
                });

                metricIndex++;
            }

            // Create event atoms if present
            if (dataPoint.Events?.Count > 0)
            {
                int eventIndex = 0;
                foreach (var evt in dataPoint.Events)
                {
                    var eventBytes = Encoding.UTF8.GetBytes(evt.Message);
                    if (eventBytes.Length > MaxAtomSize)
                        eventBytes = eventBytes.Take(MaxAtomSize).ToArray();

                    var eventHash = SHA256.HashData(eventBytes);
                    var eventAtom = new AtomData
                    {
                        AtomicValue = eventBytes,
                        ContentHash = eventHash,
                        Modality = "telemetry",
                        Subtype = $"event-{evt.Severity.ToLower()}",
                        ContentType = "text/plain",
                        CanonicalText = evt.Message,
                        Metadata = $"{{\"severity\":\"{evt.Severity}\",\"code\":\"{evt.Code}\",\"timestamp\":\"{evt.Timestamp:O}\"}}"
                    };

                    if (!atoms.Any(a => a.ContentHash.SequenceEqual(eventHash)))
                    {
                        atoms.Add(eventAtom);
                    }

                    compositions.Add(new AtomComposition
                    {
                        ParentAtomHash = deviceHash,
                        ComponentAtomHash = eventHash,
                        SequenceIndex = eventIndex++,
                        Position = new SpatialPosition
                        {
                            X = 0,
                            Y = eventIndex,
                            Z = 1, // Z=1 for events vs Z=0 for metrics
                            M = evt.Timestamp.ToUnixTimeMilliseconds()
                        }
                    });
                }
            }

            sw.Stop();

            return new AtomizationResult
            {
                Atoms = atoms,
                Compositions = compositions,
                ProcessingInfo = new ProcessingMetadata
                {
                    TotalAtoms = atoms.Count,
                    UniqueAtoms = atoms.Count,
                    DurationMs = sw.ElapsedMilliseconds,
                    AtomizerType = nameof(TelemetryAtomizer),
                    DetectedFormat = $"Telemetry - {dataPoint.Metrics.Count} metrics, {dataPoint.Events?.Count ?? 0} events",
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

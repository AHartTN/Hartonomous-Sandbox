using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Models.Telemetry;
using Hartonomous.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes telemetry data streams from IoT devices, sensors, SCADA systems, etc.
/// Supports time-series data with temporal positioning and real-time ingestion.
/// </summary>
public class TelemetryAtomizer : BaseAtomizer<TelemetryDataPoint>
{
    public TelemetryAtomizer(ILogger<TelemetryAtomizer> logger) : base(logger) { }

    public override int Priority => 60;

    public override bool CanHandle(string contentType, string? fileExtension)
    {
        return false; // Invoked explicitly via TelemetryDataPoint
    }

    protected override async Task AtomizeCoreAsync(
        TelemetryDataPoint dataPoint,
        SourceMetadata source,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var deviceIdBytes = Encoding.UTF8.GetBytes(dataPoint.DeviceId);
        var deviceHash = HashUtilities.ComputeSHA256(deviceIdBytes);
        
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

        int metricIndex = 0;
        foreach (var metric in dataPoint.Metrics)
        {
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
                var strVal = metric.Value?.ToString() ?? "";
                valueBytes = Encoding.UTF8.GetBytes(strVal);
                if (valueBytes.Length > MaxAtomSize)
                    valueBytes = valueBytes.Take(MaxAtomSize).ToArray();
                valueType = "string";
            }

            var metricHash = CreateContentAtom(
                valueBytes,
                "telemetry",
                $"metric-{valueType}",
                $"{metric.Name}={metric.Value}{metric.Unit}",
                $"{{\"name\":\"{metric.Name}\",\"value\":{JsonSerializer.Serialize(metric.Value)},\"unit\":\"{metric.Unit}\",\"type\":\"{valueType}\"}}",
                atoms);

            CreateAtomComposition(
                deviceHash,
                metricHash,
                dataPoint.SequenceNumber,
                compositions,
                x: metricIndex,
                y: dataPoint.SequenceNumber,
                z: 0,
                m: dataPoint.Timestamp.ToUnixTimeMilliseconds());

            metricIndex++;
        }

        if (dataPoint.Events?.Count > 0)
        {
            int eventIndex = 0;
            foreach (var evt in dataPoint.Events)
            {
                var eventBytes = Encoding.UTF8.GetBytes(evt.Message);
                if (eventBytes.Length > MaxAtomSize)
                    eventBytes = eventBytes.Take(MaxAtomSize).ToArray();

                var eventHash = CreateContentAtom(
                    eventBytes,
                    "telemetry",
                    $"event-{evt.Severity.ToLower()}",
                    evt.Message,
                    $"{{\"severity\":\"{evt.Severity}\",\"code\":\"{evt.Code}\",\"timestamp\":\"{evt.Timestamp:O}\"}}",
                    atoms);

                CreateAtomComposition(
                    deviceHash,
                    eventHash,
                    eventIndex++,
                    compositions,
                    x: 0,
                    y: eventIndex,
                    z: 1,
                    m: evt.Timestamp.ToUnixTimeMilliseconds());
            }
        }

        await Task.CompletedTask;
    }

    protected override string GetDetectedFormat() => "telemetry stream";

    protected override string GetModality() => "telemetry";

    protected override byte[] GetFileMetadataBytes(TelemetryDataPoint input, SourceMetadata source)
    {
        return Encoding.UTF8.GetBytes($"telemetry:{input.DeviceId}:{input.Metrics.Count}");
    }

    protected override string GetCanonicalFileText(TelemetryDataPoint input, SourceMetadata source)
    {
        return $"{input.DeviceId} ({input.Metrics.Count} metrics)";
    }

    protected override string GetFileMetadataJson(TelemetryDataPoint input, SourceMetadata source)
    {
        return $"{{\"deviceId\":\"{input.DeviceId}\",\"deviceType\":\"{input.DeviceType}\",\"metricCount\":{input.Metrics.Count},\"eventCount\":{input.Events?.Count ?? 0}}}";
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Models.Telemetry;
using Hartonomous.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Atomizers;

public class TelemetryStreamAtomizer : BaseAtomizer<TelemetryBatch>
{
    public TelemetryStreamAtomizer(ILogger<TelemetryStreamAtomizer> logger) : base(logger) { }

    public override int Priority => 60;

    public override bool CanHandle(string contentType, string? fileExtension) => false;

    protected override async Task AtomizeCoreAsync(
        TelemetryBatch batch,
        SourceMetadata source,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var sensorBytes = Encoding.UTF8.GetBytes(batch.SensorId);
        var sensorHash = HashUtilities.ComputeSHA256(sensorBytes);
        
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

            var timestampBytes = BitConverter.GetBytes(reading.Timestamp.Ticks);
            var timestampHash = CreateContentAtom(
                timestampBytes,
                "telemetry",
                "timestamp",
                reading.Timestamp.ToString("O"),
                $"{{\"ticks\":{reading.Timestamp.Ticks},\"iso\":\"{reading.Timestamp:O}\"}}",
                atoms);

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

            var valueHash = CreateContentAtom(
                valueBytes,
                "telemetry",
                $"value-{reading.ValueType.ToString().ToLowerInvariant()}",
                valueCanonical,
                $"{{\"type\":\"{reading.ValueType}\",\"unit\":\"{batch.Unit}\",\"quality\":{reading.Quality}}}",
                atoms);

            var measurementBytes = Encoding.UTF8.GetBytes($"{batch.SensorId}:{reading.Timestamp.Ticks}");
            var measurementHash = CreateContentAtom(
                measurementBytes,
                "telemetry",
                "measurement",
                $"{batch.SensorId}@{reading.Timestamp:O}={valueCanonical}",
                $"{{\"sensorId\":\"{batch.SensorId}\",\"timestamp\":\"{reading.Timestamp:O}\",\"value\":\"{valueCanonical}\"}}",
                atoms);

            CreateAtomComposition(sensorHash, measurementHash, measurementIndex, compositions,
                x: 0, y: measurementIndex, z: 0, m: reading.Timestamp.Ticks / 10000000.0);
            
            CreateAtomComposition(measurementHash, timestampHash, 0, compositions);
            CreateAtomComposition(measurementHash, valueHash, 1, compositions, x: 1);

            measurementIndex++;
        }

        await Task.CompletedTask;
    }

    protected override string GetDetectedFormat() => "telemetry batch";
    protected override string GetModality() => "telemetry";

    protected override byte[] GetFileMetadataBytes(TelemetryBatch input, SourceMetadata source)
    {
        return Encoding.UTF8.GetBytes($"telemetry-batch:{input.SensorId}:{input.Readings.Count}");
    }

    protected override string GetCanonicalFileText(TelemetryBatch input, SourceMetadata source)
    {
        return $"{input.SensorId} ({input.Readings.Count} readings)";
    }

    protected override string GetFileMetadataJson(TelemetryBatch input, SourceMetadata source)
    {
        return $"{{\"sensorId\":\"{input.SensorId}\",\"sensorType\":\"{input.SensorType}\",\"readingCount\":{input.Readings.Count},\"unit\":\"{input.Unit}\"}}";
    }
}

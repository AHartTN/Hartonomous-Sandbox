using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Utilities;

namespace ModelIngestion.Content.Extractors;

/// <summary>
/// Transforms telemetry event payloads (JSON lines, metrics snapshots) into atom requests.
/// </summary>
public sealed class TelemetryContentExtractor : IContentExtractor
{
    public bool CanHandle(ContentExtractionContext context)
        => context.SourceType == ContentSourceType.Telemetry && context.TelemetryEvents is { Count: > 0 };

    public Task<ContentExtractionResult> ExtractAsync(ContentExtractionContext context, CancellationToken cancellationToken)
    {
        if (context.TelemetryEvents is null)
        {
            throw new InvalidOperationException("Telemetry extraction requires event payloads.");
        }

        var baseMetadata = new MetadataEnvelope(context.Metadata)
            .Set("telemetryEventCount", context.TelemetryEvents.Count);

        var diagnostics = baseMetadata.AsStrings();

        var requests = new List<AtomIngestionRequest>(context.TelemetryEvents.Count);
        foreach (var evt in context.TelemetryEvents)
        {
            var trimmed = evt?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            string hashSource = trimmed.Length > 512 ? trimmed[..512] : trimmed;
            var eventMetadata = new MetadataEnvelope(context.Metadata)
                .Set("contentType", "application/json")
                .Set("approxLength", trimmed.Length)
                .Set("sourceType", TryGetMetadataValue(context.Metadata, "sourceType") ?? "telemetry");

            var sourceType = TryGetMetadataValue(context.Metadata, "sourceType") ?? "telemetry";

            var request = new AtomIngestionRequestBuilder()
                .WithCanonicalText(trimmed)
                .WithModality("telemetry", "json")
                .WithSource(sourceType, TryGetMetadataValue(context.Metadata, "uri"))
                .WithHash(hashSource, sourceType)
                .WithMetadata(eventMetadata)
                .Build();

            requests.Add(request);
        }

        return Task.FromResult(new ContentExtractionResult(requests, diagnostics));
    }

    private static string? TryGetMetadataValue(IDictionary<string, string> metadata, string key)
        => metadata.TryGetValue(key, out var value) ? value : null;
}

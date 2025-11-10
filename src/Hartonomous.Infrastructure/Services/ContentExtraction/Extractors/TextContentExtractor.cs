using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Utilities;

namespace Hartonomous.Infrastructure.Services.ContentExtraction.Extractors;

/// <summary>
/// Extracts canonical text from plain-text sources (txt, md, json, yaml, etc.).
/// </summary>
public sealed class TextContentExtractor : IContentExtractor
{
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/plain",
        "text/markdown",
        "application/json",
        "application/x-yaml",
        "application/xml",
        "text/xml",
        "text/csv"
    };

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt",
        ".log",
        ".md",
        ".json",
        ".yaml",
        ".yml",
        ".xml",
        ".csv"
    };

    public bool CanHandle(ContentExtractionContext context)
    {
        if (context.SourceType == ContentSourceType.Telemetry)
        {
            return false; // explicit telemetry extractor handles this
        }

        if (!string.IsNullOrWhiteSpace(context.ContentType) && SupportedTypes.Contains(context.ContentType))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(context.Extension) && SupportedExtensions.Contains(context.Extension))
        {
            return true;
        }

        // Default fallback for unknown types when no other extractor claims it
        return context.ContentStream != null;
    }

    public async Task<ContentExtractionResult> ExtractAsync(ContentExtractionContext context, CancellationToken cancellationToken)
    {
        if (context.ContentStream is null)
        {
            throw new InvalidOperationException("Text extraction requires a content stream.");
        }

        context.ResetStream();
        using var reader = new StreamReader(context.ContentStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 8192, leaveOpen: true);
        var text = await reader.ReadToEndAsync().ConfigureAwait(false);

        var metadata = new MetadataEnvelope(context.Metadata)
            .Set("length", text.Length)
            .Set("contentType", context.ContentType ?? "text/plain");

        var metadataStrings = metadata.AsStrings();

        var request = new AtomIngestionRequestBuilder()
            .WithCanonicalText(text)
            .WithModality("text", metadataStrings.TryGetValue("contentType", out var type) ? type : "text/plain")
            .WithSource(metadataStrings.TryGetValue("sourceType", out var sourceType) ? sourceType : "unknown",
                metadataStrings.TryGetValue("filePath", out var file) ? file : null)
            .WithHash(text, compute: false)
            .WithMetadata(metadata)
            .Build();

        return new ContentExtractionResult(new[] { request }, metadataStrings);
    }
}

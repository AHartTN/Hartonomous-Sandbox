using System;
using System.Collections.Generic;
using System.IO;

namespace Hartonomous.Infrastructure.Services.ContentExtraction;

/// <summary>
/// Represents the resolved context passed to a content extractor.
/// </summary>
public sealed class ContentExtractionContext : IDisposable
{
    public ContentExtractionContext(
        ContentSourceType sourceType,
        Stream? contentStream,
        string? fileName,
        string? contentType,
        IReadOnlyDictionary<string, string>? metadata,
        IReadOnlyList<string>? telemetryEvents)
    {
        SourceType = sourceType;
        ContentStream = contentStream;
        FileName = fileName;
        ContentType = contentType;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : CopyMetadata(metadata);
        TelemetryEvents = telemetryEvents;
    }

    public ContentSourceType SourceType { get; }

    public Stream? ContentStream { get; private set; }

    public string? FileName { get; }

    public string? ContentType { get; }

    public IDictionary<string, string> Metadata { get; }

    public IReadOnlyList<string>? TelemetryEvents { get; }

    public string? Extension => string.IsNullOrWhiteSpace(FileName) ? null : Path.GetExtension(FileName);

    public void ResetStream()
    {
        if (ContentStream?.CanSeek == true)
        {
            ContentStream.Position = 0;
        }
    }

    public Stream EnsureSeekableClone()
    {
        if (ContentStream is MemoryStream memory && memory.CanSeek)
        {
            memory.Position = 0;
            return memory;
        }

        if (ContentStream is null)
        {
            throw new InvalidOperationException("No content stream available.");
        }

        var clone = new MemoryStream();
        ContentStream.Position = 0;
        ContentStream.CopyTo(clone);
        clone.Position = 0;
        ContentStream.Dispose();
        ContentStream = clone;
        return clone;
    }

    public void Dispose()
    {
        ContentStream?.Dispose();
        ContentStream = null;
    }

    private static IDictionary<string, string> CopyMetadata(IReadOnlyDictionary<string, string> source)
    {
        var result = new Dictionary<string, string>(source.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in source)
        {
            result[key] = value;
        }

        return result;
    }
}

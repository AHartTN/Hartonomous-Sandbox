using System;
using System.Collections.Generic;
using System.IO;

namespace ModelIngestion.Content;

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
        IDictionary<string, string>? metadata,
        IReadOnlyList<string>? telemetryEvents)
    {
        SourceType = sourceType;
        ContentStream = contentStream;
        FileName = fileName;
        ContentType = contentType;
        Metadata = metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
}

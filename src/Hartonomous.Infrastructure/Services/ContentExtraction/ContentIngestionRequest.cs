using System;
using System.Collections.Generic;
using System.IO;

namespace Hartonomous.Infrastructure.Services.ContentExtraction;

/// <summary>
/// Request object describing universal content ingestion parameters.
/// </summary>
public sealed class ContentIngestionRequest
{
    private ContentIngestionRequest(ContentSourceType sourceType)
    {
        SourceType = sourceType;
    }

    public ContentSourceType SourceType { get; }

    public string? Path { get; init; }

    public Uri? Uri { get; init; }

    public Stream? Stream { get; init; }

    public string? ContentType { get; init; }

    public string? CodeLanguage { get; init; }

    public IReadOnlyList<string>? TelemetryEvents { get; init; }

    public IDictionary<string, string>? Metadata { get; init; }

    public static ContentIngestionRequest ForFile(string path, string? contentType = null, IDictionary<string, string>? metadata = null)
        => new(ContentSourceType.File)
        {
            Path = path,
            ContentType = contentType,
            Metadata = metadata
        };

    public static ContentIngestionRequest ForHttp(Uri uri, string? contentType = null, IDictionary<string, string>? metadata = null)
        => new(ContentSourceType.Http)
        {
            Uri = uri,
            ContentType = contentType,
            Metadata = metadata
        };

    public static ContentIngestionRequest ForStream(Stream stream, string? contentType = null, IDictionary<string, string>? metadata = null)
        => new(ContentSourceType.Stream)
        {
            Stream = stream,
            ContentType = contentType,
            Metadata = metadata
        };

    public static ContentIngestionRequest ForTelemetry(IReadOnlyList<string> events, IDictionary<string, string>? metadata = null)
        => new(ContentSourceType.Telemetry)
        {
            TelemetryEvents = events,
            Metadata = metadata
        };
}

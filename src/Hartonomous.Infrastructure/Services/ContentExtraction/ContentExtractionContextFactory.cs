using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.ContentExtraction;

internal sealed class ContentExtractionContextFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ContentExtractionContextFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task<ContentExtractionContext> CreateAsync(ContentIngestionRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return request.SourceType switch
        {
            ContentSourceType.File => await CreateFileContextAsync(request, cancellationToken).ConfigureAwait(false),
            ContentSourceType.Http => await CreateHttpContextAsync(request, cancellationToken).ConfigureAwait(false),
            ContentSourceType.Stream => await CreateStreamContextAsync(request, cancellationToken).ConfigureAwait(false),
            ContentSourceType.Telemetry => CreateTelemetryContext(request),
            _ => throw new NotSupportedException($"Unsupported content source: {request.SourceType}")
        };
    }

    private static async Task<ContentExtractionContext> CreateFileContextAsync(ContentIngestionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Path))
        {
            throw new ArgumentException("File path must be supplied for file ingestion.", nameof(request));
        }

        if (!File.Exists(request.Path))
        {
            throw new FileNotFoundException("Input file not found", request.Path);
        }

        var metadata = new MetadataEnvelope(request.Metadata)
            .Set("sourceType", "file")
            .Set("filePath", request.Path);

        var fileName = Path.GetFileName(request.Path);
        var extension = Path.GetExtension(fileName);
        var contentType = request.ContentType ?? MimeTypeMap.FromExtension(extension);
        var memory = await ReadToMemoryStreamAsync(() => File.OpenRead(request.Path), cancellationToken).ConfigureAwait(false);

        metadata.Set("fileSize", memory.Length);

        return new ContentExtractionContext(ContentSourceType.File, memory, fileName, contentType, metadata.AsStrings(), null);
    }

    private async Task<ContentExtractionContext> CreateHttpContextAsync(ContentIngestionRequest request, CancellationToken cancellationToken)
    {
        if (request.Uri is null)
        {
            throw new ArgumentException("Request URI must be supplied for HTTP ingestion.", nameof(request));
        }

        var client = _httpClientFactory.CreateClient("ContentIngestion");
        using var response = await client.GetAsync(request.Uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var metadata = new MetadataEnvelope(request.Metadata)
            .Set("sourceType", "http")
            .Set("uri", request.Uri.ToString())
            .Set("statusCode", (int)response.StatusCode);

        if (response.Content.Headers.ContentLength is long contentLength)
        {
            metadata.Set("contentLength", contentLength);
        }

        var contentType = request.ContentType ?? response.Content.Headers.ContentType?.MediaType;
        var fileName = Path.GetFileName(request.Uri.AbsolutePath);
        var memory = await ReadToMemoryStreamAsync(() => response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken).ConfigureAwait(false);

        return new ContentExtractionContext(ContentSourceType.Http, memory, fileName, contentType, metadata.AsStrings(), null);
    }

    private static async Task<ContentExtractionContext> CreateStreamContextAsync(ContentIngestionRequest request, CancellationToken cancellationToken)
    {
        if (request.Stream is null)
        {
            throw new ArgumentException("Stream must be supplied for stream ingestion.", nameof(request));
        }

        var metadata = new MetadataEnvelope(request.Metadata)
            .Set("sourceType", "stream");

        var memory = await ReadToMemoryStreamAsync(() => Task.FromResult(request.Stream), cancellationToken).ConfigureAwait(false);

        return new ContentExtractionContext(ContentSourceType.Stream, memory, null, request.ContentType, metadata.AsStrings(), null);
    }

    private static ContentExtractionContext CreateTelemetryContext(ContentIngestionRequest request)
    {
        if (request.TelemetryEvents is null || request.TelemetryEvents.Count == 0)
        {
            throw new ArgumentException("Telemetry ingestion requires at least one event payload.", nameof(request));
        }

        var metadata = new MetadataEnvelope(request.Metadata)
            .Set("sourceType", "telemetry")
            .Set("eventCount", request.TelemetryEvents.Count);

        return new ContentExtractionContext(ContentSourceType.Telemetry, null, null, "application/json", metadata.AsStrings(), request.TelemetryEvents);
    }

    private static async Task<MemoryStream> ReadToMemoryStreamAsync(Func<Task<Stream>> streamFactory, CancellationToken cancellationToken)
    {
        await using var source = await streamFactory().ConfigureAwait(false);
        var memory = new MemoryStream();
        await source.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        memory.Position = 0;
        return memory;
    }

    private static async Task<MemoryStream> ReadToMemoryStreamAsync(Func<Stream> streamFactory, CancellationToken cancellationToken)
    {
        await using var source = streamFactory();
        var memory = new MemoryStream();
        await source.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        memory.Position = 0;
        return memory;
    }
}

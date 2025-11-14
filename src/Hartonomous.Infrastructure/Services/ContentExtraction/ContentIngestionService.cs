using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Services.ContentExtraction;

/// <summary>
/// Central orchestrator that converts heterogeneous content sources into atom ingestion requests.
/// </summary>
public sealed class ContentIngestionService
{
    private readonly ILogger<ContentIngestionService> _logger;
    private readonly IEnumerable<IContentExtractor> _extractors;
    private readonly IAtomIngestionService _atomIngestionService;
    private readonly ContentExtractionContextFactory _contextFactory;

    public ContentIngestionService(
        ILogger<ContentIngestionService> logger,
        IHttpClientFactory httpClientFactory,
        IEnumerable<IContentExtractor> extractors,
        IAtomIngestionService atomIngestionService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _contextFactory = new ContentExtractionContextFactory(httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory)));
        _extractors = extractors?.ToArray() ?? throw new ArgumentNullException(nameof(extractors));
        _atomIngestionService = atomIngestionService ?? throw new ArgumentNullException(nameof(atomIngestionService));
    }

    public async Task<ContentIngestionResult> IngestAsync(ContentIngestionRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        using var context = await _contextFactory.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        var extractor = ResolveExtractor(context);

        _logger.LogInformation("Resolved extractor {ExtractorType} for content source {Source}", extractor.GetType().Name, DescribeSource(request));

        var extraction = await extractor.ExtractAsync(context, cancellationToken).ConfigureAwait(false);
        var atomResults = new List<AtomIngestionResult>(extraction.AtomRequests.Count);

        foreach (var atomRequest in extraction.AtomRequests)
        {
            var result = await _atomIngestionService.IngestAsync(atomRequest, cancellationToken).ConfigureAwait(false);
            atomResults.Add(result);
        }

        var sourceId = request.Path ?? request.Uri?.ToString() ?? request.Stream?.GetHashCode().ToString("X") ?? Guid.NewGuid().ToString("N");
        var diagnostics = extraction.Diagnostics;

        return new ContentIngestionResult(sourceId, atomResults, diagnostics);
    }

    private IContentExtractor ResolveExtractor(ContentExtractionContext context)
    {
        foreach (var extractor in _extractors)
        {
            if (extractor.CanHandle(context))
            {
                return extractor;
            }
        }

        throw new NotSupportedException($"No extractor registered for source type {context.SourceType} with content type '{context.ContentType}' and extension '{context.Extension}'.");        
    }

    private static string DescribeSource(ContentIngestionRequest request)
    {
        return request.SourceType switch
        {
            ContentSourceType.File => request.Path ?? "file",
            ContentSourceType.Http => request.Uri?.ToString() ?? "http",
            ContentSourceType.Stream => "stream",
            ContentSourceType.Telemetry => $"telemetry:{request.TelemetryEvents?.Count ?? 0}",
            _ => "unknown"
        };
    }
}

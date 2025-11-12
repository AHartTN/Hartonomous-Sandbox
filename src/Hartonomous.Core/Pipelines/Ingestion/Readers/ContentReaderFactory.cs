using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Pipelines.Ingestion.Readers;

/// <summary>
/// Factory for creating content readers based on URI schemes
/// Supports: file://, http://, https://, s3://, db://, kafka://, etc.
/// </summary>
public sealed class ContentReaderFactory : IContentReaderFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<string, Func<string, Dictionary<string, object>?, IContentReader>> _readerFactories;

    public ContentReaderFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        
        _readerFactories = new Dictionary<string, Func<string, Dictionary<string, object>?, IContentReader>>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["file"] = (uri, opts) => CreateFileReader(uri),
            ["http"] = (uri, opts) => CreateHttpReader(uri, opts),
            ["https"] = (uri, opts) => CreateHttpReader(uri, opts),
            // TODO: Add S3, Database, Kafka readers
        };
    }

    public IContentReader CreateReader(string sourceUri, Dictionary<string, object>? options = null)
    {
        if (string.IsNullOrWhiteSpace(sourceUri))
            throw new ArgumentException("Source URI cannot be null or empty", nameof(sourceUri));

        // Parse URI scheme
        var uri = new Uri(sourceUri, UriKind.RelativeOrAbsolute);
        
        string scheme;
        if (uri.IsAbsoluteUri)
        {
            scheme = uri.Scheme;
        }
        else
        {
            // Assume local file path
            scheme = "file";
            sourceUri = new Uri(System.IO.Path.GetFullPath(sourceUri)).AbsoluteUri;
        }

        if (!_readerFactories.TryGetValue(scheme, out var factory))
        {
            throw new NotSupportedException(
                $"No reader available for scheme '{scheme}'. " +
                $"Supported schemes: {string.Join(", ", _readerFactories.Keys)}");
        }

        return factory(sourceUri, options);
    }

    public bool CanHandle(string sourceUri)
    {
        try
        {
            var uri = new Uri(sourceUri, UriKind.RelativeOrAbsolute);
            var scheme = uri.IsAbsoluteUri ? uri.Scheme : "file";
            return _readerFactories.ContainsKey(scheme);
        }
        catch
        {
            return false;
        }
    }

    private IContentReader CreateFileReader(string sourceUri)
    {
        var uri = new Uri(sourceUri);
        var localPath = uri.LocalPath;
        return new FileSystemReader(localPath, _loggerFactory.CreateLogger<FileSystemReader>());
    }

    private IContentReader CreateHttpReader(string sourceUri, Dictionary<string, object>? options)
    {
        // TODO: Implement HttpReader for streaming HTTP responses
        throw new NotImplementedException("HTTP reader not yet implemented");
    }
}

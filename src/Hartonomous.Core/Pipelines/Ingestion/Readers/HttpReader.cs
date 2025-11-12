using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Pipelines.Ingestion.Readers;

/// <summary>
/// HTTP/HTTPS content reader with streaming, retry logic, and resume support
/// 
/// Features:
/// - Streaming downloads with backpressure via IAsyncEnumerable
/// - Automatic retry with exponential backoff
/// - Resume from offset for failed downloads (Range header)
/// - Content-Type detection from response headers
/// - Content-Length tracking for progress reporting
/// - Custom headers and authentication support
/// </summary>
public sealed class HttpReader : IContentReader
{
    private readonly Uri _uri;
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpReader> _logger;
    private readonly int _maxRetries;
    private readonly TimeSpan _initialRetryDelay;
    private readonly Dictionary<string, string> _customHeaders;

    private string? _contentType;
    private long? _contentLength;
    private bool _supportsRangeRequests;

    public string SourceUri => _uri.ToString();
    public string ContentType => _contentType ?? "application/octet-stream";
    public long? ContentLength => _contentLength;

    public HttpReader(
        Uri uri,
        HttpClient httpClient,
        ILogger<HttpReader> logger,
        int maxRetries = 3,
        TimeSpan? initialRetryDelay = null,
        Dictionary<string, string>? customHeaders = null)
    {
        if (!uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) &&
            !uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"URI scheme must be http or https, got: {uri.Scheme}", nameof(uri));
        }

        _uri = uri;
        _httpClient = httpClient;
        _logger = logger;
        _maxRetries = maxRetries;
        _initialRetryDelay = initialRetryDelay ?? TimeSpan.FromSeconds(1);
        _customHeaders = customHeaders ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Initialize reader by sending HEAD request to get content metadata
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, _uri);
            AddCustomHeaders(request);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            _contentType = response.Content.Headers.ContentType?.MediaType;
            _contentLength = response.Content.Headers.ContentLength;
            _supportsRangeRequests = response.Headers.AcceptRanges?.Contains("bytes") ?? false;

            _logger.LogInformation(
                "Initialized HTTP reader: {Uri} | Type: {ContentType} | Size: {Size} bytes | Range: {SupportsRange}",
                _uri, _contentType, _contentLength, _supportsRangeRequests);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HEAD request failed for {Uri}, will attempt GET", _uri);
            // Some servers don't support HEAD, will discover metadata during GET
        }
    }

    public async IAsyncEnumerable<ReadChunk> ReadChunksAsync(
        int chunkSize = 1024 * 1024,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_contentType == null)
        {
            await InitializeAsync(cancellationToken);
        }

        long totalBytesRead = 0;
        int retryCount = 0;
        var buffer = new byte[chunkSize];

        while (true)
        {
            List<ReadChunk> chunks;
            bool isComplete;

            try
            {
                chunks = await ReadChunkBatchAsync(buffer, totalBytesRead, cancellationToken);
                isComplete = chunks.Count == 0 || 
                             (chunks.Any() && chunks.Last().Metadata.ContainsKey("isLast") && 
                              (bool)chunks.Last().Metadata["isLast"]);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                retryCount++;

                if (retryCount > _maxRetries)
                {
                    _logger.LogError(ex, "Max retries exceeded for {Uri} after {Retries} attempts", _uri, _maxRetries);
                    throw new InvalidOperationException(
                        $"Failed to download {_uri} after {_maxRetries} retries", ex);
                }

                if (!_supportsRangeRequests && totalBytesRead > 0)
                {
                    _logger.LogError(ex, "Download failed at byte {Offset} but server doesn't support Range requests", totalBytesRead);
                    throw new InvalidOperationException(
                        $"Download failed and cannot resume: {_uri} does not support Range requests", ex);
                }

                var delay = TimeSpan.FromMilliseconds(_initialRetryDelay.TotalMilliseconds * Math.Pow(2, retryCount - 1));
                _logger.LogWarning(ex,
                    "Download failed at byte {Offset}, retrying ({Retry}/{MaxRetries}) after {Delay}ms",
                    totalBytesRead, retryCount, _maxRetries, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
                continue; // Retry
            }

            // Yield chunks outside try-catch
            foreach (var chunk in chunks)
            {
                totalBytesRead += chunk.Length;
                yield return chunk;
            }

            if (isComplete)
            {
                _logger.LogInformation("Completed download: {Uri} | {Bytes} bytes", _uri, totalBytesRead);
                yield break;
            }
        }
    }

    private async Task<List<ReadChunk>> ReadChunkBatchAsync(
        byte[] buffer,
        long currentOffset,
        CancellationToken cancellationToken)
    {
        var chunks = new List<ReadChunk>();

        using var request = new HttpRequestMessage(HttpMethod.Get, _uri);
        AddCustomHeaders(request);

        if (currentOffset > 0 && _supportsRangeRequests)
        {
            request.Headers.Range = new RangeHeaderValue(currentOffset, null);
            _logger.LogDebug("Resuming download from byte {Offset}", currentOffset);
        }

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        // Update metadata from response if not already set
        if (_contentType == null)
        {
            _contentType = response.Content.Headers.ContentType?.MediaType;
        }
        if (_contentLength == null)
        {
            _contentLength = response.Content.Headers.ContentLength;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        // Stream chunks
        int bytesRead;
        long bytesReadInBatch = 0;
        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            var chunkData = new byte[bytesRead];
            Array.Copy(buffer, chunkData, bytesRead);

            var isLastChunk = _contentLength.HasValue &&
                             (currentOffset + bytesReadInBatch + bytesRead >= _contentLength.Value);

            var chunk = new ReadChunk(
                data: chunkData,
                offset: currentOffset + bytesReadInBatch,
                length: bytesRead,
                memory: new ReadOnlyMemory<byte>(chunkData),
                metadata: new Dictionary<string, object>
                {
                    ["url"] = _uri.ToString(),
                    ["contentType"] = _contentType ?? "application/octet-stream",
                    ["contentLength"] = _contentLength ?? -1,
                    ["isLast"] = isLastChunk
                });

            chunks.Add(chunk);
            bytesReadInBatch += bytesRead;

            if (isLastChunk)
            {
                break;
            }
        }

        return chunks;
    }

    public async Task<byte[]> ReadAllBytesAsync(CancellationToken cancellationToken = default)
    {
        if (_contentType == null)
        {
            await InitializeAsync(cancellationToken);
        }

        if (_contentLength.HasValue && _contentLength.Value > 100 * 1024 * 1024)
        {
            _logger.LogWarning(
                "Reading large HTTP resource into memory: {Uri} ({Size} MB). Consider using ReadChunksAsync() for streaming.",
                _uri, _contentLength.Value / (1024 * 1024));
        }

        int retryCount = 0;

        while (true)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, _uri);
                AddCustomHeaders(request);

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                // Update metadata
                if (_contentType == null)
                {
                    _contentType = response.Content.Headers.ContentType?.MediaType;
                }
                if (_contentLength == null)
                {
                    _contentLength = response.Content.Headers.ContentLength;
                }

                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                
                _logger.LogInformation("Downloaded {Uri}: {Bytes} bytes", _uri, bytes.Length);
                
                return bytes;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                retryCount++;

                if (retryCount > _maxRetries)
                {
                    _logger.LogError(ex, "Max retries exceeded for {Uri} after {Retries} attempts", _uri, _maxRetries);
                    throw new InvalidOperationException(
                        $"Failed to download {_uri} after {_maxRetries} retries", ex);
                }

                var delay = TimeSpan.FromMilliseconds(_initialRetryDelay.TotalMilliseconds * Math.Pow(2, retryCount - 1));
                _logger.LogWarning(ex,
                    "Download failed, retrying ({Retry}/{MaxRetries}) after {Delay}ms",
                    retryCount, _maxRetries, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
                // Loop continues to retry
            }
        }
    }

    private void AddCustomHeaders(HttpRequestMessage request)
    {
        foreach (var (key, value) in _customHeaders)
        {
            request.Headers.TryAddWithoutValidation(key, value);
        }
    }

    public void Dispose()
    {
        // HttpClient is injected, don't dispose it
    }
}

/// <summary>
/// Factory for creating HTTP readers with shared HttpClient
/// </summary>
public sealed class HttpReaderFactory
{
    private readonly HttpClient _httpClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly int _maxRetries;
    private readonly TimeSpan _timeout;

    public HttpReaderFactory(
        ILoggerFactory loggerFactory,
        int maxRetries = 3,
        TimeSpan? timeout = null)
    {
        _loggerFactory = loggerFactory;
        _maxRetries = maxRetries;
        _timeout = timeout ?? TimeSpan.FromMinutes(5);

        _httpClient = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 10,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        })
        {
            Timeout = _timeout
        };

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Hartonomous/1.0 (Multimodal Ingestion)");
    }

    public HttpReader CreateReader(
        Uri uri,
        Dictionary<string, string>? customHeaders = null)
    {
        return new HttpReader(
            uri,
            _httpClient,
            _loggerFactory.CreateLogger<HttpReader>(),
            _maxRetries,
            customHeaders: customHeaders);
    }

    public HttpReader CreateReader(
        string url,
        Dictionary<string, string>? customHeaders = null)
    {
        return CreateReader(new Uri(url), customHeaders);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

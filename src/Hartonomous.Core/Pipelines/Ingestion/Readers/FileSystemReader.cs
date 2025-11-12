using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Pipelines.Ingestion.Readers;

/// <summary>
/// Reads content from local or network file systems with chunked streaming.
/// Supports large files via IAsyncEnumerable backpressure.
/// </summary>
public sealed class FileSystemReader : IContentReader
{
    private readonly string _filePath;
    private readonly ILogger<FileSystemReader>? _logger;
    private FileStream? _stream;
    private bool _disposed;

    public FileSystemReader(string filePath, ILogger<FileSystemReader>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}", filePath);

        _filePath = filePath;
        _logger = logger;
    }

    public string SourceUri => $"file://{Path.GetFullPath(_filePath)}";

    public string? ContentType
    {
        get
        {
            var extension = Path.GetExtension(_filePath).ToLowerInvariant();
            return MimeTypeMap.GetMimeType(extension);
        }
    }

    public long? ContentLength
    {
        get
        {
            try
            {
                return new FileInfo(_filePath).Length;
            }
            catch
            {
                return null;
            }
        }
    }

    public async IAsyncEnumerable<ReadChunk> ReadChunksAsync(
        int chunkSize = 1024 * 1024,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _stream = new FileStream(
            _filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: chunkSize,
            useAsync: true);

        var buffer = new byte[chunkSize];
        long offset = 0;
        int bytesRead;

        _logger?.LogDebug("Reading file {FilePath} in {ChunkSize} byte chunks", _filePath, chunkSize);

        while ((bytesRead = await _stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            var chunkData = new byte[bytesRead];
            Buffer.BlockCopy(buffer, 0, chunkData, 0, bytesRead);

            var metadata = new Dictionary<string, object>
            {
                ["fileName"] = Path.GetFileName(_filePath),
                ["fileExtension"] = Path.GetExtension(_filePath),
                ["isLast"] = _stream.Position >= _stream.Length
            };

            yield return new ReadChunk(
                chunkData,
                offset,
                bytesRead,
                new ReadOnlyMemory<byte>(chunkData),
                metadata);

            offset += bytesRead;
        }

        _logger?.LogDebug("Completed reading file {FilePath}, {TotalBytes} bytes in {ChunkCount} chunks",
            _filePath, offset, offset / chunkSize + 1);
    }

    public async Task<byte[]> ReadAllBytesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var fileInfo = new FileInfo(_filePath);
        if (fileInfo.Length > 100 * 1024 * 1024) // 100MB warning
        {
            _logger?.LogWarning(
                "Reading entire file {FilePath} ({Size} MB) into memory. Consider using ReadChunksAsync for large files.",
                _filePath, fileInfo.Length / (1024.0 * 1024.0));
        }

        return await File.ReadAllBytesAsync(_filePath, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _stream?.Dispose();
        _stream = null;
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Simple MIME type mapper (minimal implementation - extend as needed)
/// </summary>
internal static class MimeTypeMap
{
    private static readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        [".txt"] = "text/plain",
        [".md"] = "text/markdown",
        [".json"] = "application/json",
        [".xml"] = "application/xml",
        [".html"] = "text/html",
        [".css"] = "text/css",
        [".js"] = "application/javascript",
        [".csv"] = "text/csv",
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".bmp"] = "image/bmp",
        [".webp"] = "image/webp",
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".ogg"] = "audio/ogg",
        [".mp4"] = "video/mp4",
        [".webm"] = "video/webm",
        [".avi"] = "video/x-msvideo"
    };

    public static string? GetMimeType(string extension)
    {
        return _map.TryGetValue(extension, out var mimeType) ? mimeType : null;
    }
}

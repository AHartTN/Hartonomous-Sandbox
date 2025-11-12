using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Pipelines.Ingestion;

/// <summary>
/// READER LAYER: Raw source → byte streams
/// 
/// Readers abstract heterogeneous content sources (files, HTTP, S3, databases, streams)
/// into a unified streaming interface. Support backpressure via IAsyncEnumerable.
/// 
/// Examples:
/// - FileSystemReader: Reads local/network files with chunking
/// - HttpReader: Streams HTTP responses with retry logic
/// - S3Reader: Streams from cloud storage with resumption
/// - DatabaseReader: Exports query results as streams
/// - KafkaReader: Consumes message topics
/// </summary>
public interface IContentReader : IDisposable
{
    /// <summary>
    /// Source identifier for provenance tracking (e.g., file path, HTTP URL, S3 bucket+key)
    /// </summary>
    string SourceUri { get; }

    /// <summary>
    /// Detected content type (MIME type) if available
    /// </summary>
    string? ContentType { get; }

    /// <summary>
    /// Total content length in bytes (null if unknown/streaming)
    /// </summary>
    long? ContentLength { get; }

    /// <summary>
    /// Read content as chunked stream with backpressure support.
    /// Yields ReadChunk instances containing data, offset, metadata.
    /// </summary>
    IAsyncEnumerable<ReadChunk> ReadChunksAsync(
        int chunkSize = 1024 * 1024, // 1MB default
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Read entire content into memory (use for small sources only)
    /// </summary>
    Task<byte[]> ReadAllBytesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a chunk of content read from a source
/// </summary>
public sealed class ReadChunk
{
    public ReadChunk(byte[] data, long offset, int length, ReadOnlyMemory<byte> memory, Dictionary<string, object>? metadata = null)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Offset = offset;
        Length = length;
        Memory = memory;
        Metadata = metadata ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Raw bytes (may contain more data than Length indicates)
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Memory view of the actual chunk data
    /// </summary>
    public ReadOnlyMemory<byte> Memory { get; }

    /// <summary>
    /// Byte offset from start of source (for resumption)
    /// </summary>
    public long Offset { get; }

    /// <summary>
    /// Actual bytes in this chunk (may be less than Data.Length for final chunk)
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Chunk-specific metadata (e.g., compression, encryption, checksums)
    /// </summary>
    public Dictionary<string, object> Metadata { get; }

    /// <summary>
    /// Is this the final chunk?
    /// </summary>
    public bool IsLastChunk => Metadata.ContainsKey("isLast") && (bool)Metadata["isLast"];
}

/// <summary>
/// Factory for creating readers based on source URIs
/// </summary>
public interface IContentReaderFactory
{
    /// <summary>
    /// Create a reader for the given source URI
    /// Supports: file://, http://, https://, s3://, db://, kafka://, etc.
    /// </summary>
    IContentReader CreateReader(string sourceUri, Dictionary<string, object>? options = null);

    /// <summary>
    /// Check if a reader can be created for the given URI scheme
    /// </summary>
    bool CanHandle(string sourceUri);
}

/// <summary>
/// Progress notification for chunked reading operations
/// </summary>
public sealed class ReadProgress
{
    public long BytesRead { get; init; }
    public long? TotalBytes { get; init; }
    public double? PercentComplete => TotalBytes.HasValue && TotalBytes > 0 
        ? (double)BytesRead / TotalBytes.Value * 100.0 
        : null;
    public int ChunksRead { get; init; }
    public TimeSpan Elapsed { get; init; }
    public double BytesPerSecond => Elapsed.TotalSeconds > 0 
        ? BytesRead / Elapsed.TotalSeconds 
        : 0;
}

using System;
using System.IO;

namespace Hartonomous.Core.Interfaces.Ingestion;

/// <summary>
/// Detects file types via magic bytes, extensions, and content analysis.
/// </summary>
public interface IFileTypeDetector
{
    /// <summary>
    /// Detect file type from content.
    /// </summary>
    /// <param name="content">File content (at least first 512 bytes recommended)</param>
    /// <param name="fileName">Optional filename for extension hints</param>
    /// <returns>Detected file type info</returns>
    FileTypeInfo Detect(ReadOnlySpan<byte> content, string? fileName = null);

    /// <summary>
    /// Detect file type from stream.
    /// </summary>
    FileTypeInfo Detect(Stream stream, string? fileName = null);
}

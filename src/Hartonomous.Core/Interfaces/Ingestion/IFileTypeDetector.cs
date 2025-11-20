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

/// <summary>
/// File type detection result.
/// </summary>
public class FileTypeInfo
{
    /// <summary>
    /// Primary content type (MIME).
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// File category for routing.
    /// </summary>
    public required FileCategory Category { get; init; }

    /// <summary>
    /// Specific format detected.
    /// </summary>
    public string? SpecificFormat { get; init; }

    /// <summary>
    /// Detection confidence (0.0 to 1.0).
    /// </summary>
    public required double Confidence { get; init; }

    /// <summary>
    /// File extension (without dot).
    /// </summary>
    public string? Extension { get; init; }

    /// <summary>
    /// Additional format-specific metadata.
    /// </summary>
    public string? Metadata { get; init; }
}

/// <summary>
/// High-level file categories for atomizer routing.
/// </summary>
public enum FileCategory
{
    Unknown = 0,
    
    // Text-based
    Text = 1,
    Code = 2,
    Markdown = 3,
    Json = 4,
    Xml = 5,
    Yaml = 6,
    
    // Images
    ImageRaster = 10,
    ImageVector = 11,
    
    // Audio
    Audio = 20,
    
    // Video
    Video = 30,
    
    // Documents
    DocumentPdf = 40,
    DocumentWord = 41,
    DocumentExcel = 42,
    DocumentPowerPoint = 43,
    
    // Archives
    Archive = 50,
    
    // AI Models
    ModelGguf = 60,
    ModelSafeTensors = 61,
    ModelOnnx = 62,
    ModelPyTorch = 63,
    ModelTensorFlow = 64,
    
    // Databases
    Database = 70,
    
    // Executables
    Executable = 80,
    
    // Binary
    Binary = 90
}

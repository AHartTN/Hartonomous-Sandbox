namespace Hartonomous.Core.Interfaces.Ingestion;

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

namespace Hartonomous.Core.Interfaces.Ingestion;

/// <summary>
/// Individual atom with content and metadata.
/// </summary>
public class AtomData
{
    /// <summary>
    /// Raw atomic value (max 64 bytes, enforced by schema).
    /// </summary>
    public required byte[] AtomicValue { get; init; }

    /// <summary>
    /// SHA-256 content hash (32 bytes) - content-addressable key.
    /// </summary>
    public required byte[] ContentHash { get; init; }

    /// <summary>
    /// Modality classification.
    /// </summary>
    public required string Modality { get; init; }

    /// <summary>
    /// Subtype within modality (e.g., "rgba-pixel", "float32-weight", "utf8-char").
    /// </summary>
    public string? Subtype { get; init; }

    /// <summary>
    /// Content type (MIME-like).
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Canonical text representation (for text atoms).
    /// </summary>
    public string? CanonicalText { get; init; }

    /// <summary>
    /// Extensible metadata (JSON).
    /// </summary>
    public string? Metadata { get; init; }
}

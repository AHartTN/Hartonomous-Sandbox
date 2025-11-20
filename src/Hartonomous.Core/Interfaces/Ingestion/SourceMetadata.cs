namespace Hartonomous.Core.Interfaces.Ingestion;

/// <summary>
/// Source metadata for provenance tracking.
/// </summary>
public class SourceMetadata
{
    /// <summary>
    /// Original filename.
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// Source URI (file path, URL, etc.).
    /// </summary>
    public string? SourceUri { get; init; }

    /// <summary>
    /// Content type (MIME).
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Original file size in bytes.
    /// </summary>
    public long? SizeBytes { get; init; }

    /// <summary>
    /// Source type classification.
    /// </summary>
    public string? SourceType { get; init; }

    /// <summary>
    /// Tenant ID for multi-tenancy.
    /// </summary>
    public required int TenantId { get; init; }

    /// <summary>
    /// Additional metadata (JSON).
    /// </summary>
    public string? Metadata { get; init; }
}

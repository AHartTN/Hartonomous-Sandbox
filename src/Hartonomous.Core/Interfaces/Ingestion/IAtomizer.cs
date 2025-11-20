using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces.Ingestion;

/// <summary>
/// Base interface for all atomization strategies.
/// Atomizers decompose content into 64-byte maximum atoms with full spatial tracking.
/// </summary>
/// <typeparam name="TInput">Input content type (byte[], Stream, etc.)</typeparam>
public interface IAtomizer<TInput>
{
    /// <summary>
    /// Atomize content into 64-byte atoms with spatial positions.
    /// </summary>
    /// <param name="input">Content to atomize</param>
    /// <param name="metadata">Source metadata for provenance</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Atomization result with atoms and spatial structure</returns>
    Task<AtomizationResult> AtomizeAsync(
        TInput input,
        SourceMetadata metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if this atomizer can handle the given content type.
    /// </summary>
    bool CanHandle(string contentType, string? fileExtension = null);

    /// <summary>
    /// Get atomizer priority (higher = preferred when multiple atomizers match).
    /// </summary>
    int Priority { get; }
}

/// <summary>
/// Result of atomization containing all extracted atoms and their relationships.
/// </summary>
public class AtomizationResult
{
    /// <summary>
    /// All unique atoms extracted (max 64 bytes each).
    /// </summary>
    public required List<AtomData> Atoms { get; init; }

    /// <summary>
    /// Spatial composition - relationships between atoms.
    /// Maps parent atom to its component atoms with positions.
    /// </summary>
    public required List<AtomComposition> Compositions { get; init; }

    /// <summary>
    /// Metadata about the atomization process.
    /// </summary>
    public required ProcessingMetadata ProcessingInfo { get; init; }

    /// <summary>
    /// Child sources (e.g., files extracted from archives).
    /// These trigger recursive atomization.
    /// </summary>
    public List<ChildSource>? ChildSources { get; init; }
}

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

/// <summary>
/// Spatial relationship between atoms.
/// </summary>
public class AtomComposition
{
    /// <summary>
    /// Parent atom hash (the whole).
    /// </summary>
    public required byte[] ParentAtomHash { get; init; }

    /// <summary>
    /// Component atom hash (the part).
    /// </summary>
    public required byte[] ComponentAtomHash { get; init; }

    /// <summary>
    /// Sequential index (for ordered structures).
    /// </summary>
    public required long SequenceIndex { get; init; }

    /// <summary>
    /// Spatial position as GEOMETRY point.
    /// X, Y, Z, M coordinates encode position in structure.
    /// </summary>
    public required SpatialPosition Position { get; init; }
}

/// <summary>
/// Spatial position in 3D+M space.
/// </summary>
public class SpatialPosition
{
    /// <summary>
    /// X coordinate (e.g., column, pixel X, tensor X).
    /// </summary>
    public required double X { get; init; }

    /// <summary>
    /// Y coordinate (e.g., row, pixel Y, tensor Y).
    /// </summary>
    public required double Y { get; init; }

    /// <summary>
    /// Z coordinate (e.g., depth, layer, tensor Z).
    /// </summary>
    public double Z { get; init; }

    /// <summary>
    /// M coordinate (measure - e.g., time, importance, confidence).
    /// </summary>
    public double? M { get; init; }

    /// <summary>
    /// Convert to SQL Server GEOMETRY Well-Known Text.
    /// </summary>
    public string ToWkt()
    {
        if (M.HasValue)
            return $"POINT ({X} {Y} {Z} {M.Value})";
        return $"POINT ({X} {Y} {Z})";
    }
}

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

/// <summary>
/// Processing metadata about atomization.
/// </summary>
public class ProcessingMetadata
{
    /// <summary>
    /// Total atoms extracted.
    /// </summary>
    public required int TotalAtoms { get; init; }

    /// <summary>
    /// Unique atoms (after deduplication).
    /// </summary>
    public required int UniqueAtoms { get; init; }

    /// <summary>
    /// Processing duration in milliseconds.
    /// </summary>
    public required long DurationMs { get; init; }

    /// <summary>
    /// Atomizer type used.
    /// </summary>
    public required string AtomizerType { get; init; }

    /// <summary>
    /// Format detected.
    /// </summary>
    public string? DetectedFormat { get; init; }

    /// <summary>
    /// Any warnings encountered.
    /// </summary>
    public List<string>? Warnings { get; init; }
}

/// <summary>
/// Child source for recursive atomization (e.g., file in archive).
/// </summary>
public class ChildSource
{
    /// <summary>
    /// Child content.
    /// </summary>
    public required byte[] Content { get; init; }

    /// <summary>
    /// Child metadata.
    /// </summary>
    public required SourceMetadata Metadata { get; init; }

    /// <summary>
    /// Parent atom hash (the archive/container).
    /// </summary>
    public required byte[] ParentAtomHash { get; init; }
}

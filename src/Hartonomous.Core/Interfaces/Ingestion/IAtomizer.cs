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

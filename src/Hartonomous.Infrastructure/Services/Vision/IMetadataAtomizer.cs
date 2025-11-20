using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.Vision;

/// <summary>
/// Service for atomizing metadata values into atoms with referential integrity.
/// Converts metadata (camera make, ISO, title, etc.) into character/numeric atoms
/// and links them to content atoms via AtomComposition.
/// </summary>
public interface IMetadataAtomizer
{
    /// <summary>
    /// Atomize metadata and create compositions linking metadata atoms to content atoms.
    /// </summary>
    /// <param name="metadata">Extracted metadata to atomize</param>
    /// <param name="contentAtomIds">IDs of content atoms (pixels, samples, etc.) to link to</param>
    /// <param name="source">Source identifier for provenance</param>
    /// <returns>List of metadata atom IDs created</returns>
    Task<List<Guid>> AtomizeMetadataAsync(
        MediaMetadata metadata, 
        List<Guid> contentAtomIds, 
        string source,
        CancellationToken cancellationToken = default);
}

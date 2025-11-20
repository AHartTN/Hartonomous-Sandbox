using System.Collections.Generic;

namespace Hartonomous.Core.Interfaces.Ingestion;

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

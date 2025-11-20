namespace Hartonomous.Core.Interfaces.Ingestion;

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

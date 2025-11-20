namespace Hartonomous.Core.Interfaces.Ingestion;

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

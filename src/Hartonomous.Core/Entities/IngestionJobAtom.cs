namespace Hartonomous.Core.Entities;

/// <summary>
/// Associates an ingestion job with the atoms it produced or referenced.
/// Provides a many-to-many link enabling tracking of which atoms were created or reused during ingestion.
/// </summary>
public class IngestionJobAtom
{
    /// <summary>
    /// Gets or sets the unique identifier for the ingestion job atom association.
    /// </summary>
    public long IngestionJobAtomId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the ingestion job.
    /// </summary>
    public long IngestionJobId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the atom.
    /// </summary>
    public long AtomId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this atom was a duplicate (already existed) rather than newly created.
    /// </summary>
    public bool WasDuplicate { get; set; }

    /// <summary>
    /// Gets or sets additional notes or metadata about this association (e.g., extraction details, warnings).
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the ingestion job.
    /// </summary>
    public IngestionJob IngestionJob { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the atom.
    /// </summary>
    public Atom Atom { get; set; } = null!;
}

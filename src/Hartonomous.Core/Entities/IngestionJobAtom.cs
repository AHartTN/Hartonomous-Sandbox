namespace Hartonomous.Core.Entities;

/// <summary>
/// Associates an ingestion job with the atoms it produced or referenced.
/// </summary>
public class IngestionJobAtom
{
    public long IngestionJobAtomId { get; set; }

    public long IngestionJobId { get; set; }

    public long AtomId { get; set; }

    public bool WasDuplicate { get; set; }

    public string? Notes { get; set; }

    public IngestionJob IngestionJob { get; set; } = null!;

    public Atom Atom { get; set; } = null!;
}

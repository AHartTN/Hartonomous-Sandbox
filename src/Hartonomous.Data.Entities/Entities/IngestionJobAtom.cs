using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class IngestionJobAtom : IIngestionJobAtom
{
    public long IngestionJobAtomId { get; set; }

    public long IngestionJobId { get; set; }

    public long AtomId { get; set; }

    public bool WasDuplicate { get; set; }

    public string? Notes { get; set; }

    public virtual Atom Atom { get; set; } = null!;

    public virtual IngestionJob IngestionJob { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class IngestionJobAtoms : IIngestionJobAtoms
{
    public long IngestionJobAtomId { get; set; }

    public long IngestionJobId { get; set; }

    public long AtomId { get; set; }

    public bool WasDuplicate { get; set; }

    public string? Notes { get; set; }

    public virtual Atoms Atom { get; set; } = null!;

    public virtual IngestionJobs IngestionJob { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class IngestionJobs : IIngestionJobs
{
    public long IngestionJobId { get; set; }

    public int TenantId { get; set; }

    public long ParentAtomId { get; set; }

    public int? ModelId { get; set; }

    public string JobStatus { get; set; } = null!;

    public int AtomChunkSize { get; set; }

    public long CurrentAtomOffset { get; set; }

    public long TotalAtomsProcessed { get; set; }

    public long AtomQuota { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastUpdatedAt { get; set; }

    public virtual ICollection<IngestionJobAtoms> IngestionJobAtoms { get; set; } = new List<IngestionJobAtoms>();

    public virtual Atoms ParentAtom { get; set; } = null!;
}

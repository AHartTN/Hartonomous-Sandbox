using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface IIngestionJob
{
    long IngestionJobId { get; set; }
    int TenantId { get; set; }
    long ParentAtomId { get; set; }
    int? ModelId { get; set; }
    string JobStatus { get; set; }
    int AtomChunkSize { get; set; }
    long CurrentAtomOffset { get; set; }
    long TotalAtomsProcessed { get; set; }
    long AtomQuota { get; set; }
    string? ErrorMessage { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime LastUpdatedAt { get; set; }
    ICollection<IngestionJobAtom> IngestionJobAtoms { get; set; }
    Atom ParentAtom { get; set; }
}

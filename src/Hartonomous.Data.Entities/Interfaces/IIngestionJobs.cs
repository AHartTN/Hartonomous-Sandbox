using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IIngestionJobs
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
    ICollection<IngestionJobAtoms> IngestionJobAtoms { get; set; }
    Atoms ParentAtom { get; set; }
}

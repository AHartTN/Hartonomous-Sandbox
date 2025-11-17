using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IIngestionJobAtoms
{
    long IngestionJobAtomId { get; set; }
    long IngestionJobId { get; set; }
    long AtomId { get; set; }
    bool WasDuplicate { get; set; }
    string? Notes { get; set; }
    Atoms Atom { get; set; }
    IngestionJobs IngestionJob { get; set; }
}

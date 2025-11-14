using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IIngestionJobAtom
{
    long IngestionJobAtomId { get; set; }
    long IngestionJobId { get; set; }
    long AtomId { get; set; }
    bool WasDuplicate { get; set; }
    string? Notes { get; set; }
    Atom Atom { get; set; }
    IngestionJob IngestionJob { get; set; }
}

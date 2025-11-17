using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface ITenantAtoms
{
    int TenantId { get; set; }
    long AtomId { get; set; }
    DateTime CreatedAt { get; set; }
    Atoms Atom { get; set; }
}

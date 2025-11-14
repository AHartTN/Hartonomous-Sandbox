using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface ITenantAtom
{
    int TenantId { get; set; }
    long AtomId { get; set; }
    DateTime CreatedAt { get; set; }
    Atom Atom { get; set; }
}

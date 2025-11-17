using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class TenantAtoms : ITenantAtoms
{
    public int TenantId { get; set; }

    public long AtomId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Atoms Atom { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public partial class TenantAtom : ITenantAtom
{
    public int TenantId { get; set; }

    public long AtomId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Atom Atom { get; set; } = null!;
}

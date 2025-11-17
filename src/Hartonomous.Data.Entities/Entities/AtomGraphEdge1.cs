using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public partial class AtomGraphEdge1 : IAtomGraphEdge1
{
    public long EdgeId { get; set; }

    public long FromAtomId { get; set; }

    public long ToAtomId { get; set; }

    public string? DependencyType { get; set; }

    public string? EdgeType { get; set; }

    public double? Weight { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public int TenantId { get; set; }
}

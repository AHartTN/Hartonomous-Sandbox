using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IAtomGraphEdge1
{
    long EdgeId { get; set; }
    long FromAtomId { get; set; }
    long ToAtomId { get; set; }
    string? DependencyType { get; set; }
    string? EdgeType { get; set; }
    double? Weight { get; set; }
    string? Metadata { get; set; }
    DateTime CreatedAt { get; set; }
    int TenantId { get; set; }
}

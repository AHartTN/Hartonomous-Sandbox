using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

/// <summary>
/// Large object storage for Atoms table. Separates LOBs to disk to enable Atoms memory-optimization.
/// </summary>
public interface IAtomsLob
{
    long AtomId { get; set; }
    string? Content { get; set; }
    byte[]? ComponentStream { get; set; }
    string? Metadata { get; set; }
    string? PayloadLocator { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    Atom Atom { get; set; }
}

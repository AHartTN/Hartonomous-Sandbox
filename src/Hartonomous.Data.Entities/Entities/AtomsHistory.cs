using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

/// <summary>
/// Temporal history table for Atoms. Stores all historical versions with nonclustered columnstore for compression and fast analytics. Spatial columns excluded from columnstore due to type restrictions.
/// </summary>
public partial class AtomsHistory : IAtomsHistory
{
    public long AtomId { get; set; }

    public byte[] ContentHash { get; set; } = null!;

    public string Modality { get; set; } = null!;

    public string? Subtype { get; set; }

    public string? SourceUri { get; set; }

    public string? SourceType { get; set; }

    public byte[]? AtomicValue { get; set; }

    public string? CanonicalText { get; set; }

    public string? ContentType { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public int TenantId { get; set; }

    public long ReferenceCount { get; set; }

    public Geometry? SpatialKey { get; set; }

    public Geometry? SpatialGeography { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidTo { get; set; }
}

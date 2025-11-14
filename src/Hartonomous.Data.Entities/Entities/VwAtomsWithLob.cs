using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class VwAtomsWithLob : IVwAtomsWithLob
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

    public string? Content { get; set; }

    public byte[]? ComponentStream { get; set; }

    public string? Metadata { get; set; }

    public string? PayloadLocator { get; set; }
}

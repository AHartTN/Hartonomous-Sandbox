using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IVwAtomsWithLob
{
    long AtomId { get; set; }
    byte[] ContentHash { get; set; }
    string Modality { get; set; }
    string? Subtype { get; set; }
    string? SourceUri { get; set; }
    string? SourceType { get; set; }
    byte[]? AtomicValue { get; set; }
    string? CanonicalText { get; set; }
    string? ContentType { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime CreatedUtc { get; set; }
    DateTime? UpdatedAt { get; set; }
    bool IsActive { get; set; }
    bool IsDeleted { get; set; }
    int TenantId { get; set; }
    long ReferenceCount { get; set; }
    Geometry? SpatialKey { get; set; }
    Geometry? SpatialGeography { get; set; }
    string? Content { get; set; }
    byte[]? ComponentStream { get; set; }
    string? Metadata { get; set; }
    string? PayloadLocator { get; set; }
}

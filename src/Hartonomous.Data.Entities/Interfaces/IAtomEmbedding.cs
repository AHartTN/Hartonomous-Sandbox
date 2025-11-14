using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IAtomEmbedding
{
    long AtomEmbeddingId { get; set; }
    long AtomId { get; set; }
    SqlVector<float> EmbeddingVector { get; set; }
    int Dimension { get; set; }
    Geometry SpatialGeometry { get; set; }
    Geometry SpatialCoarse { get; set; }
    Geometry? SpatialProjection3D { get; set; }
    int SpatialBucket { get; set; }
    int? SpatialBucketX { get; set; }
    int? SpatialBucketY { get; set; }
    int? SpatialBucketZ { get; set; }
    double? SpatialProjX { get; set; }
    double? SpatialProjY { get; set; }
    double? SpatialProjZ { get; set; }
    int? ModelId { get; set; }
    string EmbeddingType { get; set; }
    string? Metadata { get; set; }
    int TenantId { get; set; }
    DateTime LastUpdated { get; set; }
    DateTime? LastComputedUtc { get; set; }
    DateTime? LastAccessedUtc { get; set; }
    DateTime CreatedAt { get; set; }
    Atom Atom { get; set; }
    ICollection<AtomEmbeddingComponent> AtomEmbeddingComponents { get; set; }
    Model? Model { get; set; }
    SemanticFeature? SemanticFeature { get; set; }
}

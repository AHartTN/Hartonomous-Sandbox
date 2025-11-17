using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IAtomEmbedding
{
    long AtomEmbeddingId { get; set; }
    long AtomId { get; set; }
    int TenantId { get; set; }
    int ModelId { get; set; }
    string EmbeddingType { get; set; }
    int Dimension { get; set; }
    Geometry SpatialKey { get; set; }
    SqlVector<float>? EmbeddingVector { get; set; }
    int? SpatialBucketX { get; set; }
    int? SpatialBucketY { get; set; }
    int? SpatialBucketZ { get; set; }
    long? HilbertValue { get; set; }
    DateTime CreatedAt { get; set; }
    Atom Atom { get; set; }
    ICollection<AtomEmbeddingComponent> AtomEmbeddingComponent { get; set; }
    Model Model { get; set; }
    SemanticFeatures? SemanticFeatures { get; set; }
}

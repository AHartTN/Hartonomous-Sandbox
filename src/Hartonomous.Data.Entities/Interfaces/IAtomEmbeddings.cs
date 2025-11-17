using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IAtomEmbeddings
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
    Atoms Atom { get; set; }
    ICollection<AtomEmbeddingComponents> AtomEmbeddingComponents { get; set; }
    Models Model { get; set; }
    SemanticFeatures? SemanticFeatures { get; set; }
}

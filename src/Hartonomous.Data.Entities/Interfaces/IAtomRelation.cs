using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IAtomRelation
{
    long AtomRelationId { get; set; }
    long SourceAtomId { get; set; }
    long TargetAtomId { get; set; }
    string RelationType { get; set; }
    int? SequenceIndex { get; set; }
    float? Weight { get; set; }
    float? Importance { get; set; }
    float? Confidence { get; set; }
    long? SpatialBucket { get; set; }
    int? SpatialBucketX { get; set; }
    int? SpatialBucketY { get; set; }
    int? SpatialBucketZ { get; set; }
    double? CoordX { get; set; }
    double? CoordY { get; set; }
    double? CoordZ { get; set; }
    double? CoordT { get; set; }
    double? CoordW { get; set; }
    Geometry? SpatialExpression { get; set; }
    string? Metadata { get; set; }
    int TenantId { get; set; }
    DateTime CreatedAt { get; set; }
    Atom SourceAtom { get; set; }
    Atom TargetAtom { get; set; }
}

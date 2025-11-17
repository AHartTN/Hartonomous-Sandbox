using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class AtomRelation : IAtomRelation
{
    public long AtomRelationId { get; set; }

    public long SourceAtomId { get; set; }

    public long TargetAtomId { get; set; }

    public string RelationType { get; set; } = null!;

    public int? SequenceIndex { get; set; }

    public float? Weight { get; set; }

    public float? Importance { get; set; }

    public float? Confidence { get; set; }

    public long? SpatialBucket { get; set; }

    public int? SpatialBucketX { get; set; }

    public int? SpatialBucketY { get; set; }

    public int? SpatialBucketZ { get; set; }

    public double? CoordX { get; set; }

    public double? CoordY { get; set; }

    public double? CoordZ { get; set; }

    public double? CoordT { get; set; }

    public double? CoordW { get; set; }

    public Geometry? SpatialExpression { get; set; }

    public string? Metadata { get; set; }

    public int TenantId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Atom SourceAtom { get; set; } = null!;

    public virtual Atom TargetAtom { get; set; } = null!;
}

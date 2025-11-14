using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class AtomEmbedding : IAtomEmbedding
{
    public long AtomEmbeddingId { get; set; }

    public long AtomId { get; set; }

    public SqlVector<float> EmbeddingVector { get; set; }

    public int Dimension { get; set; }

    public Geometry SpatialGeometry { get; set; } = null!;

    public Geometry SpatialCoarse { get; set; } = null!;

    public Geometry? SpatialProjection3D { get; set; }

    public int SpatialBucket { get; set; }

    public int? SpatialBucketX { get; set; }

    public int? SpatialBucketY { get; set; }

    public int? SpatialBucketZ { get; set; }

    public double? SpatialProjX { get; set; }

    public double? SpatialProjY { get; set; }

    public double? SpatialProjZ { get; set; }

    public int? ModelId { get; set; }

    public string EmbeddingType { get; set; } = null!;

    public string? Metadata { get; set; }

    public int TenantId { get; set; }

    public DateTime LastUpdated { get; set; }

    public DateTime? LastComputedUtc { get; set; }

    public DateTime? LastAccessedUtc { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Atom Atom { get; set; } = null!;

    public virtual ICollection<AtomEmbeddingComponent> AtomEmbeddingComponents { get; set; } = new List<AtomEmbeddingComponent>();

    public virtual Model? Model { get; set; }

    public virtual SemanticFeature? SemanticFeature { get; set; }
}

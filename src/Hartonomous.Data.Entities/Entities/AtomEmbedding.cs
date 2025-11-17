using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class AtomEmbedding : IAtomEmbedding
{
    public long AtomEmbeddingId { get; set; }

    public long AtomId { get; set; }

    public int TenantId { get; set; }

    public int ModelId { get; set; }

    public string EmbeddingType { get; set; } = null!;

    public int Dimension { get; set; }

    public Geometry SpatialKey { get; set; } = null!;

    public SqlVector<float>? EmbeddingVector { get; set; }

    public int? SpatialBucketX { get; set; }

    public int? SpatialBucketY { get; set; }

    public int? SpatialBucketZ { get; set; }

    public long? HilbertValue { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Atom Atom { get; set; } = null!;

    public virtual ICollection<AtomEmbeddingComponent> AtomEmbeddingComponent { get; set; } = new List<AtomEmbeddingComponent>();

    public virtual Model Model { get; set; } = null!;

    public virtual SemanticFeatures? SemanticFeatures { get; set; }
}

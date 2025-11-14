using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class AtomEmbeddingSpatialMetadatum : IAtomEmbeddingSpatialMetadatum
{
    public long MetadataId { get; set; }

    public int SpatialBucketX { get; set; }

    public int SpatialBucketY { get; set; }

    public int SpatialBucketZ { get; set; }

    public bool HasZ { get; set; }

    public long EmbeddingCount { get; set; }

    public double? MinProjX { get; set; }

    public double? MaxProjX { get; set; }

    public double? MinProjY { get; set; }

    public double? MaxProjY { get; set; }

    public double? MinProjZ { get; set; }

    public double? MaxProjZ { get; set; }

    public DateTime UpdatedAt { get; set; }
}

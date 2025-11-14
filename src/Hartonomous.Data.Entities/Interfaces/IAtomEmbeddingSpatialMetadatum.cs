using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IAtomEmbeddingSpatialMetadatum
{
    long MetadataId { get; set; }
    int SpatialBucketX { get; set; }
    int SpatialBucketY { get; set; }
    int SpatialBucketZ { get; set; }
    bool HasZ { get; set; }
    long EmbeddingCount { get; set; }
    double? MinProjX { get; set; }
    double? MaxProjX { get; set; }
    double? MinProjY { get; set; }
    double? MaxProjY { get; set; }
    double? MinProjZ { get; set; }
    double? MaxProjZ { get; set; }
    DateTime UpdatedAt { get; set; }
}

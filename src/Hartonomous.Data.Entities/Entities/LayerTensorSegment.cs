using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class LayerTensorSegment : ILayerTensorSegment
{
    public long LayerTensorSegmentId { get; set; }

    public long LayerId { get; set; }

    public int SegmentOrdinal { get; set; }

    public long PointOffset { get; set; }

    public int PointCount { get; set; }

    public string QuantizationType { get; set; } = null!;

    public double? QuantizationScale { get; set; }

    public double? QuantizationZeroPoint { get; set; }

    public double? Zmin { get; set; }

    public double? Zmax { get; set; }

    public double? Mmin { get; set; }

    public double? Mmax { get; set; }

    public long? MortonCode { get; set; }

    public Geometry? GeometryFootprint { get; set; }

    public byte[] RawPayload { get; set; } = null!;

    public Guid PayloadRowGuid { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ModelLayer Layer { get; set; } = null!;
}

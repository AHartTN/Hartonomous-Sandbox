using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface ILayerTensorSegment
{
    long LayerTensorSegmentId { get; set; }
    long LayerId { get; set; }
    int SegmentOrdinal { get; set; }
    long PointOffset { get; set; }
    int PointCount { get; set; }
    string QuantizationType { get; set; }
    double? QuantizationScale { get; set; }
    double? QuantizationZeroPoint { get; set; }
    double? Zmin { get; set; }
    double? Zmax { get; set; }
    double? Mmin { get; set; }
    double? Mmax { get; set; }
    long? MortonCode { get; set; }
    Geometry? GeometryFootprint { get; set; }
    byte[] RawPayload { get; set; }
    Guid PayloadRowGuid { get; set; }
    DateTime CreatedAt { get; set; }
    ModelLayer Layer { get; set; }
}

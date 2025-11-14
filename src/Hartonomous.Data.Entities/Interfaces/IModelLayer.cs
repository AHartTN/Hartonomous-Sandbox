using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IModelLayer
{
    long LayerId { get; set; }
    int ModelId { get; set; }
    int LayerIdx { get; set; }
    string? LayerName { get; set; }
    string? LayerType { get; set; }
    Geometry? WeightsGeometry { get; set; }
    string? TensorShape { get; set; }
    string? TensorDtype { get; set; }
    string? QuantizationType { get; set; }
    double? QuantizationScale { get; set; }
    double? QuantizationZeroPoint { get; set; }
    string? Parameters { get; set; }
    long? ParameterCount { get; set; }
    double? Zmin { get; set; }
    double? Zmax { get; set; }
    double? Mmin { get; set; }
    double? Mmax { get; set; }
    long? MortonCode { get; set; }
    int? PreviewPointCount { get; set; }
    double? CacheHitRate { get; set; }
    double? AvgComputeTimeMs { get; set; }
    long? LayerAtomId { get; set; }
    ICollection<CachedActivation> CachedActivations { get; set; }
    Atom? LayerAtom { get; set; }
    ICollection<LayerTensorSegment> LayerTensorSegments { get; set; }
    Model Model { get; set; }
    ICollection<TensorAtomCoefficient> TensorAtomCoefficients { get; set; }
    ICollection<TensorAtom> TensorAtoms { get; set; }
    ICollection<Weight> Weights { get; set; }
}

using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class ModelLayers : IModelLayers
{
    public long LayerId { get; set; }

    public int ModelId { get; set; }

    public int LayerIdx { get; set; }

    public string? LayerName { get; set; }

    public string? LayerType { get; set; }

    public Geometry? WeightsGeometry { get; set; }

    public string? TensorShape { get; set; }

    public string? TensorDtype { get; set; }

    public string? QuantizationType { get; set; }

    public double? QuantizationScale { get; set; }

    public double? QuantizationZeroPoint { get; set; }

    public string? Parameters { get; set; }

    public long? ParameterCount { get; set; }

    public double? Zmin { get; set; }

    public double? Zmax { get; set; }

    public double? Mmin { get; set; }

    public double? Mmax { get; set; }

    public long? MortonCode { get; set; }

    public int? PreviewPointCount { get; set; }

    public double? CacheHitRate { get; set; }

    public double? AvgComputeTimeMs { get; set; }

    public long? LayerAtomId { get; set; }

    public virtual ICollection<CachedActivations> CachedActivations { get; set; } = new List<CachedActivations>();

    public virtual Atoms? LayerAtom { get; set; }

    public virtual Models Model { get; set; } = null!;

    public virtual ICollection<TensorAtoms> TensorAtoms { get; set; } = new List<TensorAtoms>();
}

using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class TensorAtomCoefficients : ITensorAtomCoefficients
{
    public long TensorAtomId { get; set; }

    public int ModelId { get; set; }

    public int LayerIdx { get; set; }

    public int PositionX { get; set; }

    public int PositionY { get; set; }

    public int PositionZ { get; set; }

    public Geometry? SpatialKey { get; set; }

    public long? TensorAtomCoefficientId { get; set; }

    public long? ParentLayerId { get; set; }

    public string? TensorRole { get; set; }

    public float? Coefficient { get; set; }

    public virtual Models Model { get; set; } = null!;

    public virtual Atoms TensorAtom { get; set; } = null!;
}

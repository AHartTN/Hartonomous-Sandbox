using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class TensorAtomCoefficient : ITensorAtomCoefficient
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

    public virtual Model Model { get; set; } = null!;

    public virtual Atom TensorAtom { get; set; } = null!;
}

using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities.Entities;

public interface ITensorAtomCoefficient
{
    long TensorAtomId { get; set; }
    int ModelId { get; set; }
    int LayerIdx { get; set; }
    int PositionX { get; set; }
    int PositionY { get; set; }
    int PositionZ { get; set; }
    Geometry? SpatialKey { get; set; }
    long? TensorAtomCoefficientId { get; set; }
    long? ParentLayerId { get; set; }
    string? TensorRole { get; set; }
    float? Coefficient { get; set; }
    Model Model { get; set; }
    Atom TensorAtom { get; set; }
}

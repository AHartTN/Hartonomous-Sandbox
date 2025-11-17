using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface ITensorAtoms
{
    long TensorAtomId { get; set; }
    long AtomId { get; set; }
    int? ModelId { get; set; }
    long? LayerId { get; set; }
    string AtomType { get; set; }
    Geometry? SpatialSignature { get; set; }
    Geometry? GeometryFootprint { get; set; }
    string? Metadata { get; set; }
    float? ImportanceScore { get; set; }
    DateTime CreatedAt { get; set; }
    Atoms Atom { get; set; }
    ModelLayers? Layer { get; set; }
    Models? Model { get; set; }
}

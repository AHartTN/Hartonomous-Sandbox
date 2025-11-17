using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class TensorAtoms : ITensorAtoms
{
    public long TensorAtomId { get; set; }

    public long AtomId { get; set; }

    public int? ModelId { get; set; }

    public long? LayerId { get; set; }

    public string AtomType { get; set; } = null!;

    public Geometry? SpatialSignature { get; set; }

    public Geometry? GeometryFootprint { get; set; }

    public string? Metadata { get; set; }

    public float? ImportanceScore { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Atoms Atom { get; set; } = null!;

    public virtual ModelLayers? Layer { get; set; }

    public virtual Models? Model { get; set; }
}

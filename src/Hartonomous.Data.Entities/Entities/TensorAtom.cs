using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities.Entities;

public partial class TensorAtom : ITensorAtom
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

    public virtual Atom Atom { get; set; } = null!;

    public virtual ModelLayer? Layer { get; set; }

    public virtual Model? Model { get; set; }
}

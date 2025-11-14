using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class AtomComposition : IAtomComposition
{
    public long CompositionId { get; set; }

    public long SourceAtomId { get; set; }

    public long ComponentAtomId { get; set; }

    public string ComponentType { get; set; } = null!;

    public Geometry PositionKey { get; set; } = null!;

    public long? SequenceIndex { get; set; }

    public int? DimensionX { get; set; }

    public int? DimensionY { get; set; }

    public int? DimensionZ { get; set; }

    public int? DimensionM { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Atom ComponentAtom { get; set; } = null!;

    public virtual Atom SourceAtom { get; set; } = null!;
}

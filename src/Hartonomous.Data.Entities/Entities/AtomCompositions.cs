using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class AtomCompositions : IAtomCompositions
{
    public long CompositionId { get; set; }

    public long ParentAtomId { get; set; }

    public long ComponentAtomId { get; set; }

    public long SequenceIndex { get; set; }

    public Geometry? SpatialKey { get; set; }

    public virtual Atoms ComponentAtom { get; set; } = null!;

    public virtual Atoms ParentAtom { get; set; } = null!;
}

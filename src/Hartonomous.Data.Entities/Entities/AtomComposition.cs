using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class AtomComposition : IAtomComposition
{
    public long CompositionId { get; set; }

    public long ParentAtomId { get; set; }

    public long ComponentAtomId { get; set; }

    public long SequenceIndex { get; set; }

    public Geometry? SpatialKey { get; set; }

    public virtual Atom ComponentAtom { get; set; } = null!;

    public virtual Atom ParentAtom { get; set; } = null!;
}

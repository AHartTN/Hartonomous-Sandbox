using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IAtomComposition
{
    long CompositionId { get; set; }
    long ParentAtomId { get; set; }
    long ComponentAtomId { get; set; }
    long SequenceIndex { get; set; }
    Geometry? SpatialKey { get; set; }
    Atom ComponentAtom { get; set; }
    Atom ParentAtom { get; set; }
}

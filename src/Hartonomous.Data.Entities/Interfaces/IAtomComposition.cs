using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IAtomComposition
{
    long CompositionId { get; set; }
    long SourceAtomId { get; set; }
    long ComponentAtomId { get; set; }
    string ComponentType { get; set; }
    Geometry PositionKey { get; set; }
    long? SequenceIndex { get; set; }
    int? DimensionX { get; set; }
    int? DimensionY { get; set; }
    int? DimensionZ { get; set; }
    int? DimensionM { get; set; }
    string? Metadata { get; set; }
    DateTime CreatedAt { get; set; }
    Atom ComponentAtom { get; set; }
    Atom SourceAtom { get; set; }
}

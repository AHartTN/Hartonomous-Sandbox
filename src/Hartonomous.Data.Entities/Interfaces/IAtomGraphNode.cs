using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IAtomGraphNode
{
    long GraphId1ed5587659f24875930e33ed4194a3a6 { get; set; }
    string NodeIdA4600067acf04785ae52bb0b40c7d43b { get; set; }
    long AtomId { get; set; }
    string Modality { get; set; }
    string? Subtype { get; set; }
    string? SourceType { get; set; }
    string? SourceUri { get; set; }
    string? PayloadLocator { get; set; }
    string? CanonicalText { get; set; }
    string? Metadata { get; set; }
    string? Semantics { get; set; }
    Geometry? SpatialKey { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

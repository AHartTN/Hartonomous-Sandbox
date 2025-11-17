using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IAtomGraphNodes
{
    long GraphId8316578acbaa4d43b0aea45baf11ee8a { get; set; }
    string NodeIdE82207a9821c447c97a4a1ba75b3025d { get; set; }
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

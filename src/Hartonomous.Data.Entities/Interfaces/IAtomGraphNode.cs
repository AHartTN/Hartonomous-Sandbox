using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities.Entities;

public interface IAtomGraphNode
{
    long GraphId494f8ff335f24c699e7cd43e9f927f38 { get; set; }
    string NodeId4da534f68b2342b9b1d83cbdaea1bdab { get; set; }
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

using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities.Entities;

public partial class AtomGraphNode : IAtomGraphNode
{
    public long GraphId494f8ff335f24c699e7cd43e9f927f38 { get; set; }

    public string NodeId4da534f68b2342b9b1d83cbdaea1bdab { get; set; } = null!;

    public long AtomId { get; set; }

    public string Modality { get; set; } = null!;

    public string? Subtype { get; set; }

    public string? SourceType { get; set; }

    public string? SourceUri { get; set; }

    public string? PayloadLocator { get; set; }

    public string? CanonicalText { get; set; }

    public string? Metadata { get; set; }

    public string? Semantics { get; set; }

    public Geometry? SpatialKey { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

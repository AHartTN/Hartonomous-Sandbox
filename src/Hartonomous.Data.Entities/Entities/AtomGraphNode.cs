using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class AtomGraphNode : IAtomGraphNode
{
    public long GraphId1ed5587659f24875930e33ed4194a3a6 { get; set; }

    public string NodeIdA4600067acf04785ae52bb0b40c7d43b { get; set; } = null!;

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

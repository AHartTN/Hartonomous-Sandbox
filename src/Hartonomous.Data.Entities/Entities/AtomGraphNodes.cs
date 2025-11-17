using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class AtomGraphNodes : IAtomGraphNodes
{
    public long GraphId8316578acbaa4d43b0aea45baf11ee8a { get; set; }

    public string NodeIdE82207a9821c447c97a4a1ba75b3025d { get; set; } = null!;

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

using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class AtomGraphEdges : IAtomGraphEdges
{
    public long GraphId54ce94e6f75d41f68f1e2b740e8ed972 { get; set; }

    public string EdgeId6854515447584ef793d8b88940f42811 { get; set; } = null!;

    public int FromObjId44e30bdc381441b480860e5c26c9cbe6 { get; set; }

    public long FromId33a86cbf5fef41cb8bd2367f9f7fb292 { get; set; }

    public string? FromId8832880a53e242528f26affcb9f6dde9 { get; set; }

    public int ToObjId25d973ef142042ba981b6cd8bbad1beb { get; set; }

    public long ToId7d4d4ca90296499ab2a761f4032ef0c7 { get; set; }

    public string? ToId848cfeedb29947e18ecbc48b9a9fed21 { get; set; }

    public long AtomRelationId { get; set; }

    public string RelationType { get; set; } = null!;

    public double? Weight { get; set; }

    public string? Metadata { get; set; }

    public Geometry? SpatialExpression { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

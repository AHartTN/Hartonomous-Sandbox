using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities.Entities;

public partial class AtomGraphEdge : IAtomGraphEdge
{
    public long GraphId429ab56d0ebd458fb38c43c3b8267534 { get; set; }

    public string EdgeId47be36ae1c4b4f3bb7f0e08849fa977e { get; set; } = null!;

    public int FromObjId071043cc800b4943aeffc058465ef7fa { get; set; }

    public long FromIdFc1219066bdc4776b44a5a0412bcf924 { get; set; }

    public string? FromIdA3fdce16d30d4b9691ba136908f40759 { get; set; }

    public int ToObjId80f993575fbd4a9ea686d1cab73ad763 { get; set; }

    public long ToIdF2762fc34e0f412c8fc5f6ed00f26692 { get; set; }

    public string? ToId579809caf8c941d88f251a1c6ee5fd07 { get; set; }

    public long AtomRelationId { get; set; }

    public string RelationType { get; set; } = null!;

    public double? Weight { get; set; }

    public string? Metadata { get; set; }

    public Geometry? SpatialExpression { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

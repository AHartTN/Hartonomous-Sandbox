using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class AtomGraphEdge : IAtomGraphEdge
{
    public long GraphIdC0374105abf94d8690a28e3930c45799 { get; set; }

    public string EdgeIdEe9be59b11634b148dba809bd1d99150 { get; set; } = null!;

    public int FromObjIdDb1d84b22b074350a65c32aba4c92a17 { get; set; }

    public long FromId1e22d35020c54c1da39133f04210fc27 { get; set; }

    public string? FromId607e9fcfb54c4a409ab7bdc29b63f086 { get; set; }

    public int ToObjId4f26395fa31342c986b9312a47cc5f26 { get; set; }

    public long ToIdFfbd0b6425204fd2a03bd072f908138d { get; set; }

    public string? ToIdD48121dd004c41b0b88470dfc6226c34 { get; set; }

    public long AtomRelationId { get; set; }

    public string RelationType { get; set; } = null!;

    public double? Weight { get; set; }

    public string? Metadata { get; set; }

    public Geometry? SpatialExpression { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

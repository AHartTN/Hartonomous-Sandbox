using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IAtomGraphEdge
{
    long GraphIdC0374105abf94d8690a28e3930c45799 { get; set; }
    string EdgeIdEe9be59b11634b148dba809bd1d99150 { get; set; }
    int FromObjIdDb1d84b22b074350a65c32aba4c92a17 { get; set; }
    long FromId1e22d35020c54c1da39133f04210fc27 { get; set; }
    string? FromId607e9fcfb54c4a409ab7bdc29b63f086 { get; set; }
    int ToObjId4f26395fa31342c986b9312a47cc5f26 { get; set; }
    long ToIdFfbd0b6425204fd2a03bd072f908138d { get; set; }
    string? ToIdD48121dd004c41b0b88470dfc6226c34 { get; set; }
    long AtomRelationId { get; set; }
    string RelationType { get; set; }
    double? Weight { get; set; }
    string? Metadata { get; set; }
    Geometry? SpatialExpression { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

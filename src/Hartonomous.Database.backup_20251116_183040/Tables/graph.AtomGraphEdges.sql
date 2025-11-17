CREATE TABLE graph.AtomGraphEdges
(
    AtomRelationId      BIGINT          NOT NULL,
    RelationType        NVARCHAR(128)   NOT NULL,
    Weight              FLOAT           NULL,
    Metadata            JSON   NULL,
    SpatialExpression   GEOMETRY        NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_AtomGraphEdges PRIMARY KEY CLUSTERED (AtomRelationId),
    CONSTRAINT UX_AtomGraphEdges_AtomRelationId UNIQUE NONCLUSTERED (AtomRelationId),
    CONSTRAINT EC_AtomGraphEdges CONNECTION (graph.AtomGraphNodes TO graph.AtomGraphNodes)
) AS EDGE;
GO
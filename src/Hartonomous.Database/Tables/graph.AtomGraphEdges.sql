CREATE TABLE graph.AtomGraphEdges
(
    AtomRelationId      BIGINT          NOT NULL,
    RelationType        NVARCHAR(128)   NOT NULL,
    Weight              FLOAT           NULL,
    Metadata            NVARCHAR(MAX)   NULL,
    SpatialExpression   GEOMETRY        NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UX_AtomGraphEdges_AtomRelationId UNIQUE (AtomRelationId),
    CONSTRAINT EC_AtomGraphEdges CONNECTION (graph.AtomGraphNodes TO graph.AtomGraphNodes)
) AS EDGE;
GO
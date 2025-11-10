EXEC ('CREATE SCHEMA graph AUTHORIZATION dbo;');GO

CREATE TABLE graph.AtomGraphNodes
    (
        AtomId              BIGINT          NOT NULL,
        Modality            NVARCHAR(64)    NOT NULL,
        Subtype             NVARCHAR(64)    NULL,
        SourceType          NVARCHAR(128)   NULL,
        SourceUri           NVARCHAR(2048)  NULL,
        PayloadLocator      NVARCHAR(512)   NULL,
        CanonicalText       NVARCHAR(MAX)   NULL,
        Metadata            JSON            NULL,
        Semantics           JSON            NULL,
        SpatialKey          GEOMETRY        NULL,
        CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UX_AtomGraphNodes_AtomId UNIQUE (AtomId)
    ) AS NODE;GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AtomGraphNodes_Modality' AND object_id = OBJECT_ID(N'graph.AtomGraphNodes'))
BEGIN
    CREATE INDEX IX_AtomGraphNodes_Modality ON graph.AtomGraphNodes (Modality, Subtype);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'SIX_AtomGraphNodes_SpatialKey' AND object_id = OBJECT_ID(N'graph.AtomGraphNodes'))
BEGIN
    BEGIN TRY
        EXEC (N'CREATE SPATIAL INDEX SIX_AtomGraphNodes_SpatialKey ON graph.AtomGraphNodes (SpatialKey) ' +
              N'USING GEOMETRY_AUTO_GRID WITH (BOUNDING_BOX = (-1000000, -1000000, 1000000, 1000000), CELLS_PER_OBJECT = 16);');
    END TRY
    BEGIN CATCH
        END CATCH
END

CREATE TABLE graph.AtomGraphEdges
    (
        AtomRelationId      BIGINT          NOT NULL,
        RelationType        NVARCHAR(128)   NOT NULL,
        Weight              FLOAT           NULL,
        Metadata            JSON            NULL,
        SpatialExpression   GEOMETRY        NULL,
        CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UX_AtomGraphEdges_AtomRelationId UNIQUE (AtomRelationId),
        CONSTRAINT EC_AtomGraphEdges CONNECTION (graph.AtomGraphNodes TO graph.AtomGraphNodes)
    ) AS EDGE;GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AtomGraphEdges_Type' AND object_id = OBJECT_ID(N'graph.AtomGraphEdges'))
BEGIN
    CREATE INDEX IX_AtomGraphEdges_Type ON graph.AtomGraphEdges (RelationType);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AtomGraphEdges_FromTo' AND object_id = OBJECT_ID(N'graph.AtomGraphEdges'))
BEGIN
    CREATE INDEX IX_AtomGraphEdges_FromTo ON graph.AtomGraphEdges ($from_id, $to_id);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'JX_AtomGraphNodes_Semantics' AND object_id = OBJECT_ID(N'graph.AtomGraphNodes'))
BEGIN
    BEGIN TRY
        EXEC (N'CREATE JSON INDEX JX_AtomGraphNodes_Semantics ON graph.AtomGraphNodes (Semantics);');
    END TRY
    BEGIN CATCH
        END CATCH
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'SIX_AtomGraphEdges_SpatialExpression' AND object_id = OBJECT_ID(N'graph.AtomGraphEdges'))
BEGIN
    BEGIN TRY
        EXEC (N'CREATE SPATIAL INDEX SIX_AtomGraphEdges_SpatialExpression ON graph.AtomGraphEdges (SpatialExpression) ' +
              N'USING GEOMETRY_AUTO_GRID WITH (BOUNDING_BOX = (-1000000, -1000000, 1000000, 1000000), CELLS_PER_OBJECT = 16);');
    END TRY
    BEGIN CATCH
        END CATCH
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'JX_AtomGraphEdges_Metadata' AND object_id = OBJECT_ID(N'graph.AtomGraphEdges'))
BEGIN
    BEGIN TRY
        EXEC (N'CREATE JSON INDEX JX_AtomGraphEdges_Metadata ON graph.AtomGraphEdges (Metadata);');
    END TRY
    BEGIN CATCH
        END CATCH
END

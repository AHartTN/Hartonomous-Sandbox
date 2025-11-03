IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'graph')
BEGIN
    EXEC ('CREATE SCHEMA graph AUTHORIZATION dbo;');
END
GO

IF OBJECT_ID(N'graph.AtomGraphNodes', N'U') IS NULL
BEGIN
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
    ) AS NODE;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AtomGraphNodes_Modality' AND object_id = OBJECT_ID(N'graph.AtomGraphNodes'))
BEGIN
    CREATE INDEX IX_AtomGraphNodes_Modality ON graph.AtomGraphNodes (Modality, Subtype);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'SIX_AtomGraphNodes_SpatialKey' AND object_id = OBJECT_ID(N'graph.AtomGraphNodes'))
BEGIN
    BEGIN TRY
        EXEC (N'CREATE SPATIAL INDEX SIX_AtomGraphNodes_SpatialKey ON graph.AtomGraphNodes (SpatialKey) ' +
              N'USING GEOMETRY_AUTO_GRID WITH (BOUNDING_BOX = (-1000000, -1000000, 1000000, 1000000), CELLS_PER_OBJECT = 16);');
    END TRY
    BEGIN CATCH
        PRINT 'CREATE SPATIAL INDEX SIX_AtomGraphNodes_SpatialKey skipped (feature unavailable).';
    END CATCH
END
GO

IF OBJECT_ID(N'graph.AtomGraphEdges', N'U') IS NULL
BEGIN
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
    ) AS EDGE;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AtomGraphEdges_Type' AND object_id = OBJECT_ID(N'graph.AtomGraphEdges'))
BEGIN
    CREATE INDEX IX_AtomGraphEdges_Type ON graph.AtomGraphEdges (RelationType);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AtomGraphEdges_FromTo' AND object_id = OBJECT_ID(N'graph.AtomGraphEdges'))
BEGIN
    CREATE INDEX IX_AtomGraphEdges_FromTo ON graph.AtomGraphEdges ($from_id, $to_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'JX_AtomGraphNodes_Semantics' AND object_id = OBJECT_ID(N'graph.AtomGraphNodes'))
BEGIN
    BEGIN TRY
        EXEC (N'CREATE JSON INDEX JX_AtomGraphNodes_Semantics ON graph.AtomGraphNodes (Semantics);');
    END TRY
    BEGIN CATCH
        PRINT 'CREATE JSON INDEX JX_AtomGraphNodes_Semantics skipped (feature unavailable).';
    END CATCH
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'SIX_AtomGraphEdges_SpatialExpression' AND object_id = OBJECT_ID(N'graph.AtomGraphEdges'))
BEGIN
    BEGIN TRY
        EXEC (N'CREATE SPATIAL INDEX SIX_AtomGraphEdges_SpatialExpression ON graph.AtomGraphEdges (SpatialExpression) ' +
              N'USING GEOMETRY_AUTO_GRID WITH (BOUNDING_BOX = (-1000000, -1000000, 1000000, 1000000), CELLS_PER_OBJECT = 16);');
    END TRY
    BEGIN CATCH
        PRINT 'CREATE SPATIAL INDEX SIX_AtomGraphEdges_SpatialExpression skipped (feature unavailable).';
    END CATCH
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'JX_AtomGraphEdges_Metadata' AND object_id = OBJECT_ID(N'graph.AtomGraphEdges'))
BEGIN
    BEGIN TRY
        EXEC (N'CREATE JSON INDEX JX_AtomGraphEdges_Metadata ON graph.AtomGraphEdges (Metadata);');
    END TRY
    BEGIN CATCH
        PRINT 'CREATE JSON INDEX JX_AtomGraphEdges_Metadata skipped (feature unavailable).';
    END CATCH
END
GO

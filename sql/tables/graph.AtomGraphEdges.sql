-- =============================================
-- Table: graph.AtomGraphEdges
-- =============================================
-- SQL Graph edge table connecting AtomGraphNodes.
-- This table was previously managed by EF Core.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'graph')
BEGIN
    EXEC('CREATE SCHEMA graph');
END
GO

IF OBJECT_ID('graph.AtomGraphEdges', 'U') IS NOT NULL
    DROP TABLE graph.AtomGraphEdges;
GO

CREATE TABLE graph.AtomGraphEdges
(
    EdgeId          BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    EdgeType        NVARCHAR(50)    NOT NULL,
    Weight          REAL            NOT NULL DEFAULT 1.0,
    Metadata        NVARCHAR(MAX)   NULL,
    ValidFrom       DATETIME2       NULL,
    ValidTo         DATETIME2       NULL,
    CreatedUtc      DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT CK_AtomGraphEdges_EdgeType CHECK (EdgeType IN ('DerivedFrom', 'ComponentOf', 'SimilarTo', 'Uses', 'InputTo', 'OutputFrom', 'BindsToConcept')),
    CONSTRAINT CK_AtomGraphEdges_Weight CHECK (Weight >= 0.0 AND Weight <= 1.0)
) AS EDGE;
GO

CREATE INDEX IX_AtomGraphEdges_EdgeType ON graph.AtomGraphEdges(EdgeType);
GO

CREATE INDEX IX_AtomGraphEdges_Weight ON graph.AtomGraphEdges(Weight);
GO

CREATE INDEX IX_AtomGraphEdges_CreatedUtc ON graph.AtomGraphEdges(CreatedUtc);
GO

PRINT 'Created table graph.AtomGraphEdges';
GO
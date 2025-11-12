-- =============================================
-- Table: graph.AtomGraphNodes
-- =============================================
-- SQL Graph node table for Atom relationships.
-- This table was previously managed by EF Core.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'graph')
BEGIN
    EXEC('CREATE SCHEMA graph');
END
GO

IF OBJECT_ID('graph.AtomGraphNodes', 'U') IS NOT NULL
    DROP TABLE graph.AtomGraphNodes;
GO

CREATE TABLE graph.AtomGraphNodes
(
    NodeId          BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    AtomId          BIGINT          NOT NULL,
    NodeType        NVARCHAR(100)   NOT NULL,
    NodeLabel       NVARCHAR(500)   NULL,
    Properties      NVARCHAR(MAX)   NULL,
    CreatedUtc      DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedUtc      DATETIME2       NULL,

    CONSTRAINT FK_AtomGraphNodes_Atoms FOREIGN KEY (AtomId) REFERENCES dbo.Atoms(AtomId) ON DELETE CASCADE,
    CONSTRAINT CK_AtomGraphNodes_Properties_IsJson CHECK (Properties IS NULL OR ISJSON(Properties) = 1)
) AS NODE;
GO

CREATE INDEX IX_AtomGraphNodes_AtomId ON graph.AtomGraphNodes(AtomId);
GO

CREATE INDEX IX_AtomGraphNodes_NodeType ON graph.AtomGraphNodes(NodeType);
GO

CREATE INDEX IX_AtomGraphNodes_CreatedUtc ON graph.AtomGraphNodes(CreatedUtc);
GO

PRINT 'Created table graph.AtomGraphNodes';
GO
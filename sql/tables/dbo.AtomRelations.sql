-- =============================================
-- Table: dbo.AtomRelations
-- =============================================
-- Represents a directed relationship between two atoms.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.AtomRelations', 'U') IS NOT NULL
    DROP TABLE dbo.AtomRelations;
GO

CREATE TABLE dbo.AtomRelations
(
    AtomRelationId      BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SourceAtomId        BIGINT          NOT NULL,
    TargetAtomId        BIGINT          NOT NULL,
    RelationType        NVARCHAR(128)   NOT NULL,
    Weight              REAL            NULL,
    SpatialExpression   GEOMETRY        NULL,
    Metadata            NVARCHAR(MAX)   NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_AtomRelations_SourceAtom FOREIGN KEY (SourceAtomId) REFERENCES dbo.Atoms(AtomId) ON DELETE NO ACTION,
    CONSTRAINT FK_AtomRelations_TargetAtom FOREIGN KEY (TargetAtomId) REFERENCES dbo.Atoms(AtomId) ON DELETE NO ACTION,
    CONSTRAINT CK_AtomRelations_Metadata_IsJson CHECK (Metadata IS NULL OR ISJSON(Metadata) = 1)
);
GO

CREATE INDEX IX_AtomRelations_Source_Target_Type ON dbo.AtomRelations(SourceAtomId, TargetAtomId, RelationType);
GO

PRINT 'Created table dbo.AtomRelations';
GO

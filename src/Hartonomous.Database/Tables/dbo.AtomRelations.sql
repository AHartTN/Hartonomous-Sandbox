-- =============================================
-- Table: dbo.AtomRelations
-- Description: Directed relationships between atoms forming graph structures.
--              Supports provenance tracking, attention mapping, and compositional reasoning.
-- =============================================
CREATE TABLE [dbo].[AtomRelations]
(
    [AtomRelationId]     BIGINT           NOT NULL IDENTITY(1,1),
    [SourceAtomId]       BIGINT           NOT NULL,
    [TargetAtomId]       BIGINT           NOT NULL,
    [RelationType]       NVARCHAR(128)    NOT NULL,
    [Weight]             REAL             NULL,
    [SpatialExpression]  GEOMETRY         NULL,
    [Metadata]           NVARCHAR(MAX)    NULL, -- JSON column type applied via CHECK constraint
    [CreatedAt]          DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_AtomRelations] PRIMARY KEY CLUSTERED ([AtomRelationId] ASC),

    CONSTRAINT [FK_AtomRelations_SourceAtoms] 
        FOREIGN KEY ([SourceAtomId]) 
        REFERENCES [dbo].[Atoms]([AtomId]) 
        ON DELETE NO ACTION,

    CONSTRAINT [FK_AtomRelations_TargetAtoms] 
        FOREIGN KEY ([TargetAtomId]) 
        REFERENCES [dbo].[Atoms]([AtomId]) 
        ON DELETE NO ACTION,

    CONSTRAINT [CK_AtomRelations_Metadata_IsJson] 
        CHECK ([Metadata] IS NULL OR ISJSON([Metadata]) = 1)
);
GO

CREATE NONCLUSTERED INDEX [IX_AtomRelations_Source_Target_Type]
    ON [dbo].[AtomRelations]([SourceAtomId] ASC, [TargetAtomId] ASC, [RelationType] ASC);
GO
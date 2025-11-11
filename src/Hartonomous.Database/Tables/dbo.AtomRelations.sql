CREATE TABLE [dbo].[AtomRelations] (
    [AtomRelationId]   BIGINT        NOT NULL IDENTITY,
    [SourceAtomId]     BIGINT        NOT NULL,
    [TargetAtomId]     BIGINT        NOT NULL,
    [RelationType]     NVARCHAR (128)NOT NULL,
    [Weight]           REAL          NULL,
    [SpatialExpression]GEOMETRY      NULL,
    [Metadata]         NVARCHAR(MAX) NULL,
    [CreatedAt]        DATETIME2 (7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_AtomRelations] PRIMARY KEY CLUSTERED ([AtomRelationId] ASC),
    CONSTRAINT [FK_AtomRelations_Atoms_SourceAtomId] FOREIGN KEY ([SourceAtomId]) REFERENCES [dbo].[Atoms] ([AtomId]),
    CONSTRAINT [FK_AtomRelations_Atoms_TargetAtomId] FOREIGN KEY ([TargetAtomId]) REFERENCES [dbo].[Atoms] ([AtomId])
);

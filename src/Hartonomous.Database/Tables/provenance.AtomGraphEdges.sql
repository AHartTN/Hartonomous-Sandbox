-- provenance.AtomGraphEdges: Lineage tracking edges (not a graph table - regular table)
-- Separate from graph.AtomGraphEdges which is for semantic relationships
CREATE TABLE [provenance].[AtomGraphEdges] (
    [EdgeId]         BIGINT         NOT NULL IDENTITY PRIMARY KEY,
    [FromAtomId]     BIGINT         NOT NULL,
    [ToAtomId]       BIGINT         NOT NULL,
    [DependencyType] NVARCHAR (50)  NULL, -- 'DerivedFrom', 'TransformedTo', 'MergedInto'
    [EdgeType]       NVARCHAR (50)  NULL,
    [Weight]         FLOAT (53)     NULL,
    [Metadata]       NVARCHAR (MAX) NULL,
    [CreatedAt]      DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    [TenantId]       INT            NOT NULL DEFAULT (0),
    INDEX [IX_AtomGraphEdges_FromId] ([FromAtomId]),
    INDEX [IX_AtomGraphEdges_ToId] ([ToAtomId]),
    INDEX [IX_AtomGraphEdges_DependencyType] ([DependencyType])
);

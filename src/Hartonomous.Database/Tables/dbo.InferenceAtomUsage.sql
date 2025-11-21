-- =============================================
-- Table: dbo.InferenceAtomUsage
-- Description: Tracks which atoms and relations were used in each inference
-- Purpose: Links InferenceRequests to specific AtomRelations for feedback propagation
-- =============================================

CREATE TABLE [dbo].[InferenceAtomUsage]
(
    [UsageId] BIGINT NOT NULL IDENTITY(1,1),
    [InferenceRequestId] BIGINT NOT NULL,
    [AtomRelationId] BIGINT NOT NULL,
    [UsageType] NVARCHAR(50) NULL, -- e.g., 'Input', 'Reasoning', 'Output'
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_InferenceAtomUsage_CreatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_InferenceAtomUsage] PRIMARY KEY CLUSTERED ([UsageId] ASC),
    CONSTRAINT [FK_InferenceAtomUsage_InferenceRequest]
        FOREIGN KEY ([InferenceRequestId])
        REFERENCES [dbo].[InferenceRequest]([InferenceId])
        ON DELETE CASCADE,
    CONSTRAINT [FK_InferenceAtomUsage_AtomRelation]
        FOREIGN KEY ([AtomRelationId])
        REFERENCES [dbo].[AtomRelation]([AtomRelationId])
);
GO

CREATE NONCLUSTERED INDEX [IX_InferenceAtomUsage_InferenceRequestId]
    ON [dbo].[InferenceAtomUsage]([InferenceRequestId])
    INCLUDE ([AtomRelationId], [UsageType]);
GO

CREATE NONCLUSTERED INDEX [IX_InferenceAtomUsage_AtomRelationId]
    ON [dbo].[InferenceAtomUsage]([AtomRelationId])
    INCLUDE ([InferenceRequestId]);
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Tracks atoms and relations used in each inference request. Enables feedback to propagate back to specific semantic graph edges.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'InferenceAtomUsage';
GO

-- =============================================
-- Table: dbo.InferenceTracking
-- Purpose: Tracks which atoms were used in each inference request
-- Used by: sp_Analyze for velocity calculation (usage frequency)
-- =============================================

CREATE TABLE [dbo].[InferenceTracking]
(
    [InferenceTrackingId] BIGINT IDENTITY(1,1) NOT NULL,
    [InferenceId] BIGINT NOT NULL,
    [AtomId] BIGINT NOT NULL,
    [UsageType] NVARCHAR(50) NULL, -- 'input', 'context', 'output'
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_InferenceTracking] PRIMARY KEY CLUSTERED ([InferenceTrackingId] ASC),
    CONSTRAINT [FK_InferenceTracking_InferenceRequest] FOREIGN KEY ([InferenceId])
        REFERENCES [dbo].[InferenceRequest]([InferenceId]) ON DELETE CASCADE,
    CONSTRAINT [FK_InferenceTracking_Atom] FOREIGN KEY ([AtomId])
        REFERENCES [dbo].[Atom]([AtomId]) ON DELETE CASCADE
);
GO

-- Index for sp_Analyze velocity queries (count by AtomId)
CREATE NONCLUSTERED INDEX [IX_InferenceTracking_AtomId]
    ON [dbo].[InferenceTracking]([AtomId])
    INCLUDE ([InferenceId], [CreatedAt]);
GO

-- Index for inference-specific queries
CREATE NONCLUSTERED INDEX [IX_InferenceTracking_InferenceId]
    ON [dbo].[InferenceTracking]([InferenceId])
    INCLUDE ([AtomId], [UsageType]);
GO

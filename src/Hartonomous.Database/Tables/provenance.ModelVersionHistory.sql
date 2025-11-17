CREATE TABLE [provenance].[ModelVersionHistory] (
    [VersionHistoryId] BIGINT         NOT NULL IDENTITY,
    [ModelId]          INT            NOT NULL,
    [VersionTag]       NVARCHAR (50)  NOT NULL,
    [VersionHash]      NVARCHAR (64)  NULL,      -- SHA-256 hash of model binary
    [ChangeDescription] NVARCHAR (MAX) NULL,      -- What changed in this version
    [ParentVersionId]  BIGINT         NULL,      -- Previous version (null for first version)
    [PerformanceMetrics] NVARCHAR (MAX) NULL,    -- JSON: accuracy, loss, etc.
    [CreatedBy]        NVARCHAR (128) NULL,      -- User/process that created this version
    [CreatedAt]        DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    [TenantId]         INT            NOT NULL DEFAULT (0),
    CONSTRAINT [PK_ModelVersionHistory] PRIMARY KEY CLUSTERED ([VersionHistoryId] ASC),
    CONSTRAINT [FK_ModelVersionHistory_Models] FOREIGN KEY ([ModelId]) REFERENCES [dbo].[Model] ([ModelId]) ON DELETE CASCADE,
    CONSTRAINT [FK_ModelVersionHistory_ParentVersion] FOREIGN KEY ([ParentVersionId]) REFERENCES [provenance].[ModelVersionHistory] ([VersionHistoryId])
);
GO

CREATE NONCLUSTERED INDEX [IX_ModelVersionHistory_ModelId_CreatedAt] 
    ON [provenance].[ModelVersionHistory] ([ModelId], [CreatedAt] DESC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_ModelVersionHistory_ModelId_VersionTag] 
    ON [provenance].[ModelVersionHistory] ([ModelId], [VersionTag]);
GO

CREATE TABLE [dbo].[PendingActions]
(
    [ActionId] BIGINT IDENTITY(1,1) NOT NULL,
    [ActionType] NVARCHAR(100) NOT NULL,
    [SqlStatement] NVARCHAR(MAX) NULL,
    [Description] NVARCHAR(MAX) NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT ('PendingApproval'),
    [RiskLevel] NVARCHAR(20) NOT NULL DEFAULT ('medium'),
    [EstimatedImpact] NVARCHAR(20) NULL,
    [CreatedUtc] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [ApprovedUtc] DATETIME2 NULL,
    [ApprovedBy] NVARCHAR(128) NULL,
    [ExecutedUtc] DATETIME2 NULL,
    [ResultJson] NVARCHAR(MAX) NULL,
    [ErrorMessage] NVARCHAR(MAX) NULL,
    
    CONSTRAINT [PK_PendingActions] PRIMARY KEY CLUSTERED ([ActionId])
);
GO

CREATE NONCLUSTERED INDEX [IX_PendingActions_Status]
    ON [dbo].[PendingActions]([Status]);
GO

CREATE NONCLUSTERED INDEX [IX_PendingActions_Created]
    ON [dbo].[PendingActions]([CreatedUtc] DESC);
GO
GO
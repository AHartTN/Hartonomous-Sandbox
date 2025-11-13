CREATE TABLE [dbo].[Neo4jSyncLog] (
    [LogId]         BIGINT          NOT NULL IDENTITY,
    [EntityType]    NVARCHAR (50)   NOT NULL,
    [EntityId]      BIGINT          NOT NULL,
    [SyncType]      NVARCHAR (50)   NOT NULL, -- 'Create', 'Update', 'Delete'
    [SyncStatus]    NVARCHAR (50)   NOT NULL, -- 'Success', 'Failed', 'Pending', 'Retrying'
    [Response]      NVARCHAR (MAX)  NULL,
    [ErrorMessage]  NVARCHAR (MAX)  NULL,
    [RetryCount]    INT             NOT NULL DEFAULT 0,
    [SyncTimestamp] DATETIME2 (7)   NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Neo4jSyncLog] PRIMARY KEY CLUSTERED ([LogId] ASC),
    INDEX [IX_Neo4jSyncLog_Entity] ([EntityType], [EntityId], [SyncTimestamp] DESC),
    INDEX [IX_Neo4jSyncLog_Status] ([SyncStatus], [SyncTimestamp] DESC),
    INDEX [IX_Neo4jSyncLog_Timestamp] ([SyncTimestamp] DESC)
);

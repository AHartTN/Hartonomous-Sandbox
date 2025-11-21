CREATE TABLE [dbo].[Neo4jSyncDeadLetterQueue]
(
    [DeadLetterId] BIGINT IDENTITY(1,1) NOT NULL,
    [ConversationHandle] UNIQUEIDENTIFIER NOT NULL,
    [ErrorDescription] NVARCHAR(500) NOT NULL,
    [MessageBody] NVARCHAR(MAX) NOT NULL,
    [ErrorTimestamp] DATETIME2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Neo4jSyncDeadLetterQueue] PRIMARY KEY CLUSTERED ([DeadLetterId] ASC),
    INDEX [IX_Neo4jSyncDeadLetterQueue_ErrorTimestamp] ([ErrorTimestamp] DESC),
    INDEX [IX_Neo4jSyncDeadLetterQueue_ConversationHandle] ([ConversationHandle])
);
GO

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Dead letter queue for poison messages from Neo4jSyncQueue. Messages that cannot be processed (malformed XML, invalid data) are logged here with END CONVERSATION WITH ERROR instead of crashing the worker.', 
    @level0type=N'SCHEMA', @level0name=N'dbo', 
    @level1type=N'TABLE', @level1name=N'Neo4jSyncDeadLetterQueue';
GO

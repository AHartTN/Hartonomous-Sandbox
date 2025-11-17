-- =============================================
-- Background Jobs Table
-- Stores background job queue for asynchronous processing
-- =============================================
CREATE TABLE [dbo].[BackgroundJob]
(
    [JobId]           BIGINT IDENTITY(1,1) NOT NULL,
    [JobType]         NVARCHAR(128) NOT NULL,
    [Payload]         NVARCHAR(MAX) NULL,
    [Status]          INT NOT NULL DEFAULT 0, -- 0=Pending, 1=InProgress, 2=Completed, 3=Failed, 4=DeadLettered, 5=Cancelled, 6=Scheduled
    [AttemptCount]    INT NOT NULL DEFAULT 0,
    [MaxRetries]      INT NOT NULL DEFAULT 3,
    [Priority]        INT NOT NULL DEFAULT 0,
    [CreatedAtUtc]    DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    [ScheduledAtUtc]  DATETIME2(3) NULL,
    [StartedAtUtc]    DATETIME2(3) NULL,
    [CompletedAtUtc]  DATETIME2(3) NULL,
    [ResultData]      NVARCHAR(MAX) NULL,
    [ErrorMessage]    NVARCHAR(MAX) NULL,
    [ErrorStackTrace] NVARCHAR(MAX) NULL,
    [TenantId]        INT NULL,
    [CreatedBy]       NVARCHAR(256) NULL,
    [CorrelationId]   NVARCHAR(128) NULL,

    CONSTRAINT [PK_BackgroundJob] PRIMARY KEY CLUSTERED ([JobId] ASC),
    INDEX [IX_BackgroundJob_Status_Priority] NONCLUSTERED ([Status] ASC, [Priority] DESC, [CreatedAtUtc] ASC),
    INDEX [IX_BackgroundJob_ScheduledAtUtc] NONCLUSTERED ([ScheduledAtUtc] ASC) WHERE [ScheduledAtUtc] IS NOT NULL,
    INDEX [IX_BackgroundJob_TenantId] NONCLUSTERED ([TenantId] ASC) WHERE [TenantId] IS NOT NULL,
    INDEX [IX_BackgroundJob_CorrelationId] NONCLUSTERED ([CorrelationId] ASC) WHERE [CorrelationId] IS NOT NULL,
    INDEX [IX_BackgroundJob_JobType_Status] NONCLUSTERED ([JobType] ASC, [Status] ASC)
);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Background job queue for asynchronous task processing with priority-based execution and retry logic.',
    @level0type=N'SCHEMA', @level0name=N'dbo',
    @level1type=N'TABLE',  @level1name=N'BackgroundJob';
GO

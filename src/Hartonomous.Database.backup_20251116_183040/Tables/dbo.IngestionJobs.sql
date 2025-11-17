-- =============================================
-- IngestionJobs: Governed, Resumable Atomic Ingestion State Machine
-- Tracks chunked ingestion progress with quota enforcement and multi-tenancy
-- =============================================
CREATE TABLE [dbo].[IngestionJobs] (
    [IngestionJobId]     BIGINT          IDENTITY(1,1) NOT NULL,
    [TenantId]           INT             NOT NULL DEFAULT 0,  -- Job ownership
    [ParentAtomId]       BIGINT          NOT NULL,
    [ModelId]            INT             NULL,

    -- Job state
    [JobStatus]          VARCHAR(50)     NOT NULL DEFAULT 'Pending',  -- Pending, Processing, Failed, Complete

    -- Chunked processing state
    [AtomChunkSize]      INT             NOT NULL DEFAULT 1000000,
    [CurrentAtomOffset]  BIGINT          NOT NULL DEFAULT 0,
    [TotalAtomsProcessed] BIGINT         NOT NULL DEFAULT 0,

    -- Governance
    [AtomQuota]          BIGINT          NOT NULL DEFAULT 5000000000,  -- 5B atom quota

    -- Error tracking
    [ErrorMessage]       NVARCHAR(MAX)   NULL,

    -- Timestamps
    [CreatedAt]          DATETIME2(7)    NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastUpdatedAt]      DATETIME2(7)    NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_IngestionJobs] PRIMARY KEY CLUSTERED ([IngestionJobId] ASC),
    CONSTRAINT [FK_IngestionJobs_ParentAtom] FOREIGN KEY ([ParentAtomId])
        REFERENCES [dbo].[Atoms] ([AtomId]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_IngestionJobs_Status]
    ON [dbo].[IngestionJobs]([JobStatus], [TenantId])
    INCLUDE ([IngestionJobId], [ParentAtomId]);
GO

CREATE NONCLUSTERED INDEX [IX_IngestionJobs_TenantId]
    ON [dbo].[IngestionJobs]([TenantId], [CreatedAt] DESC)
    INCLUDE ([IngestionJobId], [JobStatus]);
GO

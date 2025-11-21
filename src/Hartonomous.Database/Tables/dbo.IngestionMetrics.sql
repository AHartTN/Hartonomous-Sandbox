-- =============================================
-- Table: dbo.IngestionMetrics
-- Description: Tracks bulk atom ingestion performance metrics
-- Purpose: Monitors deduplication rates, throughput, and batch processing performance
-- =============================================

CREATE TABLE [dbo].[IngestionMetrics]
(
    [MetricId]         BIGINT          IDENTITY(1,1) NOT NULL,
    [BatchId]          UNIQUEIDENTIFIER NOT NULL,
    [SourceId]         BIGINT          NULL,
    [TenantId]         INT             NOT NULL DEFAULT 0,
    [TotalAtoms]       INT             NOT NULL,
    [NewAtoms]         INT             NOT NULL,
    [DuplicateAtoms]   INT             NOT NULL,
    [DeduplicationRate] AS CAST([DuplicateAtoms] AS FLOAT) / NULLIF([TotalAtoms], 0) PERSISTED,
    [DurationMs]       INT             NOT NULL,
    [IngestedAt]       DATETIME2(7)    NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_IngestionMetrics] PRIMARY KEY CLUSTERED ([MetricId] ASC),
    CONSTRAINT [UQ_IngestionMetrics_BatchId] UNIQUE NONCLUSTERED ([BatchId])
);
GO

CREATE NONCLUSTERED INDEX [IX_IngestionMetrics_TenantId]
    ON [dbo].[IngestionMetrics]([TenantId], [IngestedAt] DESC)
    INCLUDE ([BatchId], [TotalAtoms], [NewAtoms], [DuplicateAtoms]);
GO

CREATE NONCLUSTERED INDEX [IX_IngestionMetrics_IngestedAt]
    ON [dbo].[IngestionMetrics]([IngestedAt] DESC)
    INCLUDE ([TenantId], [TotalAtoms], [DurationMs]);
GO

-- Index for high deduplication rate queries (optimization opportunities)
CREATE NONCLUSTERED INDEX [IX_IngestionMetrics_HighDedup]
    ON [dbo].[IngestionMetrics]([DeduplicationRate] DESC, [IngestedAt] DESC)
    INCLUDE ([BatchId], [TotalAtoms])
    WHERE [DeduplicationRate] > 0.5; -- 50%+ deduplication
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Tracks bulk atom ingestion performance metrics including deduplication rates, throughput, and batch processing times. Used for monitoring ingestion pipeline health and optimization.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'IngestionMetrics';
GO

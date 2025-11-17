-- =============================================
-- Table: dbo.CdcCheckpoints
-- Description: Change Data Capture checkpoints for event streaming
-- Purpose: Tracks CDC position for Event Hub consumers
-- =============================================

CREATE TABLE [dbo].[CdcCheckpoint]
(
    [ConsumerGroup] NVARCHAR(100) NOT NULL,
    [PartitionId] NVARCHAR(50) NOT NULL,
    [Offset] BIGINT NOT NULL,
    [SequenceNumber] BIGINT NOT NULL,
    [LastModified] DATETIME2(7) NOT NULL CONSTRAINT [DF_CdcCheckpoints_LastModified] DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_CdcCheckpoints] PRIMARY KEY CLUSTERED ([ConsumerGroup] ASC, [PartitionId] ASC)
);
GO

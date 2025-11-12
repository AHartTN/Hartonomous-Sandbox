-- Production-ready CDC checkpoint tracking table
-- Stores last successfully processed LSN for each CDC consumer
-- Enables resilient, resumable change event streaming across worker restarts

CREATE TABLE dbo.CdcCheckpoints
(
    ConsumerName NVARCHAR(100) NOT NULL,
    LastProcessedLsn NVARCHAR(50) NOT NULL,
    LastUpdated DATETIME2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT PK_CdcCheckpoints PRIMARY KEY CLUSTERED (ConsumerName)
);
GO

-- Index for checkpoint lookup by consumer
CREATE NONCLUSTERED INDEX IX_CdcCheckpoints_LastUpdated
ON dbo.CdcCheckpoints (LastUpdated DESC)
INCLUDE (ConsumerName, LastProcessedLsn);
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Tracks CDC checkpoint LSNs for resumable change event streaming. Used by CES consumers to recover from last known position after restart.',
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE',  @level1name = 'CdcCheckpoints';
GO

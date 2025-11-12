-- =============================================
-- Table: dbo.IngestionJobs
-- =============================================
-- Tracks ingestion operations and their metrics.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.IngestionJobs', 'U') IS NOT NULL
    DROP TABLE dbo.IngestionJobs;
GO

CREATE TABLE dbo.IngestionJobs
(
    IngestionJobId      BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    PipelineName        NVARCHAR(256)   NOT NULL,
    StartedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CompletedAt         DATETIME2       NULL,
    Status              NVARCHAR(64)    NULL,
    SourceUri           NVARCHAR(1024)  NULL,
    Metadata            NVARCHAR(MAX)   NULL,

    CONSTRAINT CK_IngestionJobs_Metadata_IsJson CHECK (Metadata IS NULL OR ISJSON(Metadata) = 1)
);
GO

PRINT 'Created table dbo.IngestionJobs';
GO

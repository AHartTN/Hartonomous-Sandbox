-- =============================================
-- Table: dbo.IngestionJobs
-- Description: Tracks ingestion operations and metrics for auditing and troubleshooting.
--              Records pipeline processing with status and metadata.
-- =============================================
CREATE TABLE [dbo].[IngestionJobs]
(
    [IngestionJobId]  BIGINT           NOT NULL IDENTITY(1,1),
    [PipelineName]    NVARCHAR(256)    NOT NULL,
    [StartedAt]       DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CompletedAt]     DATETIME2(7)     NULL,
    [Status]          NVARCHAR(64)     NULL,
    [SourceUri]       NVARCHAR(1024)   NULL,
    [Metadata]        NVARCHAR(MAX)    NULL, -- JSON column type applied via CHECK constraint

    CONSTRAINT [PK_IngestionJobs] PRIMARY KEY CLUSTERED ([IngestionJobId] ASC),

    CONSTRAINT [CK_IngestionJobs_Metadata_IsJson] 
        CHECK ([Metadata] IS NULL OR ISJSON([Metadata]) = 1)
);
GO
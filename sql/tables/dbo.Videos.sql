-- =============================================
-- Table: dbo.Videos
-- =============================================
-- Represents a video with metadata and global embeddings.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.Videos', 'U') IS NOT NULL
    DROP TABLE dbo.Videos;
GO

CREATE TABLE dbo.Videos
(
    VideoId             BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SourcePath          NVARCHAR(MAX)   NULL,
    Fps                 INT             NOT NULL,
    DurationMs          BIGINT          NOT NULL,
    ResolutionWidth     INT             NOT NULL,
    ResolutionHeight    INT             NOT NULL,
    NumFrames           BIGINT          NOT NULL,
    Format              NVARCHAR(50)    NULL,
    GlobalEmbedding     VECTOR(2048)    NULL, -- Placeholder dimension
    GlobalEmbeddingDim  INT             NULL,
    Metadata            NVARCHAR(MAX)   NULL,
    IngestionDate       DATETIME2       NULL,

    CONSTRAINT CK_Videos_Metadata_IsJson CHECK (Metadata IS NULL OR ISJSON(Metadata) = 1)
);
GO

PRINT 'Created table dbo.Videos';
GO

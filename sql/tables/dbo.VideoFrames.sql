-- =============================================
-- Table: dbo.VideoFrames
-- =============================================
-- Represents a single frame from a video with spatial and motion analysis data.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.VideoFrames', 'U') IS NOT NULL
    DROP TABLE dbo.VideoFrames;
GO

CREATE TABLE dbo.VideoFrames
(
    FrameId             BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    VideoId             BIGINT          NOT NULL,
    FrameNumber         BIGINT          NOT NULL,
    TimestampMs         BIGINT          NOT NULL,
    PixelCloud          GEOMETRY        NULL,
    ObjectRegions       GEOMETRY        NULL,
    MotionVectors       GEOMETRY        NULL,
    OpticalFlow         GEOMETRY        NULL,
    FrameEmbedding      VECTOR(768)     NULL,
    PerceptualHash      BINARY(8)       NULL,

    CONSTRAINT FK_VideoFrames_Videos FOREIGN KEY (VideoId) REFERENCES dbo.Videos(VideoId) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX IX_VideoFrames_VideoId_FrameNumber ON dbo.VideoFrames(VideoId, FrameNumber);
GO

CREATE INDEX IX_VideoFrames_VideoId_TimestampMs ON dbo.VideoFrames(VideoId, TimestampMs);
GO

PRINT 'Created table dbo.VideoFrames';
GO

CREATE TABLE [dbo].[VideoFrames] (
    [FrameId]        BIGINT         NOT NULL IDENTITY,
    [VideoId]        BIGINT         NOT NULL,
    [FrameNumber]    BIGINT         NOT NULL,
    [TimestampMs]    BIGINT         NOT NULL,
    [PixelCloud]     GEOMETRY       NULL,
    [ObjectRegions]  GEOMETRY       NULL,
    [MotionVectors]  GEOMETRY       NULL,
    [OpticalFlow]    GEOMETRY       NULL,
    [FrameEmbedding] VECTOR(1998)   NULL,
    [PerceptualHash] VARBINARY (8)  NULL,
    CONSTRAINT [PK_VideoFrames] PRIMARY KEY CLUSTERED ([FrameId] ASC),
    CONSTRAINT [FK_VideoFrames_Videos_VideoId] FOREIGN KEY ([VideoId]) REFERENCES [dbo].[Videos] ([VideoId]) ON DELETE CASCADE
);

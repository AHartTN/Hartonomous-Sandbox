CREATE TABLE [dbo].[AtomicPixels] (
    [PixelHash]      BINARY (32)   NOT NULL,
    [R]              TINYINT       NOT NULL,
    [G]              TINYINT       NOT NULL,
    [B]              TINYINT       NOT NULL,
    [A]              TINYINT       NOT NULL DEFAULT CAST(255 AS TINYINT),
    [ColorPoint]     GEOMETRY      NULL,
    [ReferenceCount] BIGINT        NOT NULL DEFAULT CAST(0 AS BIGINT),
    [FirstSeen]      DATETIME2 (7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastReferenced] DATETIME2 (7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_AtomicPixels] PRIMARY KEY CLUSTERED ([PixelHash] ASC)
);

-- =============================================
-- Table: dbo.AtomicPixels
-- Description: Unique atomic pixels with content-addressable deduplication.
--              Stores RGBA values and spatial color representation.
-- =============================================
CREATE TABLE [dbo].[AtomicPixels]
(
    [PixelHash]        BINARY(32)       NOT NULL,
    [R]                TINYINT          NOT NULL,
    [G]                TINYINT          NOT NULL,
    [B]                TINYINT          NOT NULL,
    [A]                TINYINT          NOT NULL DEFAULT (255),
    [ColorPoint]       GEOMETRY         NULL,
    [ReferenceCount]   BIGINT           NOT NULL DEFAULT (0),
    [FirstSeen]        DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastReferenced]   DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_AtomicPixels] PRIMARY KEY CLUSTERED ([PixelHash] ASC)
);
GO
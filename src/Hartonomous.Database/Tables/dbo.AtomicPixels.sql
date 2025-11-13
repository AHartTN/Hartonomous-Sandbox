CREATE TABLE [dbo].[AtomicPixels] (
    [PixelHash]      BINARY (32)   NOT NULL,
    [R]              TINYINT       NOT NULL,
    [G]              TINYINT       NOT NULL,
    [B]              TINYINT       NOT NULL,
    [A]              TINYINT       NOT NULL DEFAULT CAST(255 AS TINYINT),
    [RgbaBytes]      VARBINARY(4)  NOT NULL,  -- Raw 4-byte RGBA for reconstruction
    
    -- Spatial indexing: RGB color space as 3D coordinates
    [ColorPoint]     GEOMETRY      NULL,  -- POINT(R, G, B, A) for color-based queries
    
    -- Statistics
    [ReferenceCount] BIGINT        NOT NULL DEFAULT CAST(0 AS BIGINT),
    [FirstSeen]      DATETIME2 (7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastReferenced] DATETIME2 (7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_AtomicPixels] PRIMARY KEY CLUSTERED ([PixelHash] ASC),
    INDEX [IX_AtomicPixels_RGB] NONCLUSTERED ([R], [G], [B]),
    INDEX [IX_AtomicPixels_References] NONCLUSTERED ([ReferenceCount] DESC)
);
GO

-- Spatial index for RGB color space queries
CREATE SPATIAL INDEX [SIDX_AtomicPixels_ColorSpace]
ON [dbo].[AtomicPixels] ([ColorPoint])
USING GEOMETRY_GRID
WITH (
    BOUNDING_BOX = (0, 0, 255, 255),  -- RGBA range [0-255]
    GRIDS = (LEVEL_1 = HIGH, LEVEL_2 = HIGH, LEVEL_3 = HIGH, LEVEL_4 = HIGH),
    CELLS_PER_OBJECT = 16
);
GO

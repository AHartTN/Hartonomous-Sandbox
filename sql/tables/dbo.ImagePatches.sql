-- =============================================
-- Table: dbo.ImagePatches
-- =============================================
-- Represents fine-grained rectangular patches within an image.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.ImagePatches', 'U') IS NOT NULL
    DROP TABLE dbo.ImagePatches;
GO

CREATE TABLE dbo.ImagePatches
(
    PatchId         BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ImageId         BIGINT          NOT NULL,
    PatchX          INT             NOT NULL,
    PatchY          INT             NOT NULL,
    PatchWidth      INT             NOT NULL,
    PatchHeight     INT             NOT NULL,
    PatchRegion     GEOMETRY        NOT NULL,
    PatchEmbedding  VECTOR(768)     NULL,
    DominantColor   GEOMETRY        NULL,
    MeanIntensity   REAL            NULL,
    StdIntensity    REAL            NULL,

    CONSTRAINT FK_ImagePatches_Images FOREIGN KEY (ImageId) REFERENCES dbo.Images(ImageId) ON DELETE CASCADE
);
GO

CREATE INDEX IX_ImagePatches_ImageId_PatchX_PatchY ON dbo.ImagePatches(ImageId, PatchX, PatchY);
GO

PRINT 'Created table dbo.ImagePatches';
GO

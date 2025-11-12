-- =============================================
-- Table: dbo.Images
-- =============================================
-- Represents an image with spatial and vector representations.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.Images', 'U') IS NOT NULL
    DROP TABLE dbo.Images;
GO

CREATE TABLE dbo.Images
(
    ImageId             BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SourcePath          NVARCHAR(MAX)   NULL,
    SourceUrl           NVARCHAR(MAX)   NULL,
    Width               INT             NOT NULL,
    Height              INT             NOT NULL,
    Channels            INT             NOT NULL,
    Format              NVARCHAR(50)    NULL,
    PixelCloud          GEOMETRY        NULL,
    EdgeMap             GEOMETRY        NULL,
    ObjectRegions       GEOMETRY        NULL,
    SaliencyRegions     GEOMETRY        NULL,
    GlobalEmbedding     VECTOR(2048)    NULL, -- Placeholder dimension
    GlobalEmbeddingDim  INT             NULL,
    Metadata            NVARCHAR(MAX)   NULL,
    IngestionDate       DATETIME2       NULL,
    LastAccessed        DATETIME2       NULL,
    AccessCount         BIGINT          NOT NULL DEFAULT 0,

    CONSTRAINT CK_Images_Metadata_IsJson CHECK (Metadata IS NULL OR ISJSON(Metadata) = 1)
);
GO

PRINT 'Created table dbo.Images';
GO

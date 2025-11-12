-- =============================================
-- Table: dbo.AtomicPixels
-- =============================================
-- Represents unique atomic pixels with content-addressable deduplication.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.AtomicPixels', 'U') IS NOT NULL
    DROP TABLE dbo.AtomicPixels;
GO

CREATE TABLE dbo.AtomicPixels
(
    PixelHash       BINARY(32)      NOT NULL PRIMARY KEY,
    R               TINYINT         NOT NULL,
    G               TINYINT         NOT NULL,
    B               TINYINT         NOT NULL,
    A               TINYINT         NOT NULL DEFAULT 255,
    ColorPoint      GEOMETRY        NULL,
    ReferenceCount  BIGINT          NOT NULL DEFAULT 0,
    FirstSeen       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    LastReferenced  DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

PRINT 'Created table dbo.AtomicPixels';
GO

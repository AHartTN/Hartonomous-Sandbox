-- =============================================
-- Table: dbo.AtomPayloadStore
-- =============================================
-- Large blob storage using FILESTREAM for images, audio, video.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.AtomPayloadStore', 'U') IS NOT NULL
    DROP TABLE dbo.AtomPayloadStore;
GO

CREATE TABLE dbo.AtomPayloadStore (
    PayloadId BIGINT IDENTITY(1,1) PRIMARY KEY,
    RowGuid UNIQUEIDENTIFIER ROWGUIDCOL NOT NULL UNIQUE DEFAULT NEWSEQUENTIALID(),
    AtomId BIGINT NOT NULL,
    ContentType NVARCHAR(256) NOT NULL,
    ContentHash BINARY(32) NOT NULL,
    SizeBytes BIGINT NOT NULL,
    PayloadData VARBINARY(MAX) FILESTREAM NOT NULL,
    CreatedBy NVARCHAR(256) NULL,
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_AtomPayloadStore_Atoms FOREIGN KEY (AtomId) REFERENCES dbo.Atoms(AtomId) ON DELETE CASCADE,
    CONSTRAINT UX_AtomPayloadStore_ContentHash UNIQUE (ContentHash),
    CONSTRAINT CK_AtomPayloadStore_ContentType CHECK (ContentType LIKE '%/%'),
    CONSTRAINT CK_AtomPayloadStore_SizeBytes CHECK (SizeBytes > 0)
) FILESTREAM_ON HartonomousFileStream; -- Assuming HartonomousFileStream is the FILESTREAM filegroup
GO

CREATE INDEX IX_AtomPayloadStore_AtomId ON dbo.AtomPayloadStore (AtomId);
GO

CREATE INDEX IX_AtomPayloadStore_ContentHash ON dbo.AtomPayloadStore (ContentHash);
GO

PRINT 'Created table dbo.AtomPayloadStore';
GO
-- AtomPayloadStore: Large blob storage using FILESTREAM for images, audio, video
-- Enables direct file system access via SqlFileStream API for high-performance I/O
-- Each row stores one payload (atom body) with content addressing via RowGuid

CREATE TABLE dbo.AtomPayloadStore (
    PayloadId BIGINT IDENTITY(1,1) PRIMARY KEY,
    RowGuid UNIQUEIDENTIFIER ROWGUIDCOL NOT NULL UNIQUE DEFAULT NEWSEQUENTIALID(),
    
    -- Atom reference
    AtomId BIGINT NOT NULL,
    
    -- Payload metadata
    ContentType NVARCHAR(256) NOT NULL, -- MIME type: image/jpeg, audio/wav, video/mp4, etc.
    ContentHash BINARY(32) NOT NULL, -- SHA-256 hash for deduplication
    SizeBytes BIGINT NOT NULL,
    
    -- FILESTREAM column for large payloads
    PayloadData VARBINARY(MAX) FILESTREAM NOT NULL,
    
    -- Provenance
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(256),
    
    -- Indexes
    INDEX IX_AtomPayloadStore_AtomId (AtomId),
    INDEX IX_AtomPayloadStore_ContentHash (ContentHash),
    INDEX IX_AtomPayloadStore_RowGuid (RowGuid)
) FILESTREAM_ON HartonomousFileStream;
GO

-- Foreign key to Atoms table
ALTER TABLE dbo.AtomPayloadStore
ADD CONSTRAINT FK_AtomPayloadStore_Atoms
FOREIGN KEY (AtomId) REFERENCES dbo.Atoms(AtomId)
ON DELETE CASCADE;
GO

-- Unique constraint for deduplication (one ContentHash per table)
CREATE UNIQUE INDEX UX_AtomPayloadStore_ContentHash
ON dbo.AtomPayloadStore(ContentHash);
GO

-- CHECK constraint for valid content types
ALTER TABLE dbo.AtomPayloadStore
ADD CONSTRAINT CK_AtomPayloadStore_ContentType
CHECK (ContentType LIKE '%/%'); -- MIME type pattern: type/subtype
GO

-- CHECK constraint for positive size
ALTER TABLE dbo.AtomPayloadStore
ADD CONSTRAINT CK_AtomPayloadStore_SizeBytes
CHECK (SizeBytes > 0);
GO

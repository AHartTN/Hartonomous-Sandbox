-- AtomPayloadStore: Large blob storage using FILESTREAM for images, audio, video
-- Enables direct file system access via SqlFileStream API for high-performance I/O
-- Each row stores one payload (atom body) with content addressing via RowGuid

CREATE TABLE [dbo].[AtomPayloadStore]
(
    [PayloadId] BIGINT IDENTITY(1,1) NOT NULL,
    [RowGuid] UNIQUEIDENTIFIER ROWGUIDCOL NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [AtomId] BIGINT NOT NULL,
    [ContentType] NVARCHAR(256) NOT NULL,
    [ContentHash] BINARY(32) NOT NULL,
    [SizeBytes] BIGINT NOT NULL,
    [PayloadData] VARBINARY(MAX) FILESTREAM NOT NULL,
    [CreatedUtc] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedBy] NVARCHAR(256) NULL,
    
    CONSTRAINT [PK_AtomPayloadStore] PRIMARY KEY CLUSTERED ([PayloadId]),
    CONSTRAINT [UQ_AtomPayloadStore_RowGuid] UNIQUE NONCLUSTERED ([RowGuid]),
    CONSTRAINT [UQ_AtomPayloadStore_ContentHash] UNIQUE NONCLUSTERED ([ContentHash]),
    CONSTRAINT [FK_AtomPayloadStore_Atoms] FOREIGN KEY ([AtomId])
        REFERENCES [dbo].[Atoms]([AtomId])
        ON DELETE CASCADE,
    CONSTRAINT [CK_AtomPayloadStore_ContentType]
        CHECK ([ContentType] LIKE '%/%'),
    CONSTRAINT [CK_AtomPayloadStore_SizeBytes]
        CHECK ([SizeBytes] > 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_AtomPayloadStore_AtomId]
    ON [dbo].[AtomPayloadStore]([AtomId]);
GO

CREATE NONCLUSTERED INDEX [IX_AtomPayloadStore_ContentHash]
    ON [dbo].[AtomPayloadStore]([ContentHash]);
GO
CREATE TABLE [dbo].[AtomPayloadStore] (
    [PayloadId]   BIGINT           NOT NULL IDENTITY,
    [RowGuid]     UNIQUEIDENTIFIER ROWGUIDCOL NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [AtomId]      BIGINT           NOT NULL,
    [ContentType] NVARCHAR (256)   NOT NULL,
    [ContentHash] BINARY (32)      NOT NULL,
    [SizeBytes]   BIGINT           NOT NULL,
    [PayloadData] VARBINARY (MAX)  FILESTREAM NOT NULL,
    [CreatedBy]   NVARCHAR (256)   NULL,
    [CreatedUtc]  DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_AtomPayloadStore] PRIMARY KEY CLUSTERED ([PayloadId] ASC),
    CONSTRAINT [UQ_AtomPayloadStore_RowGuid] UNIQUE NONCLUSTERED ([RowGuid]),
    CONSTRAINT [CK_AtomPayloadStore_ContentType] CHECK ([ContentType] LIKE '%/%'),
    CONSTRAINT [CK_AtomPayloadStore_SizeBytes] CHECK ([SizeBytes]>(0)),
    CONSTRAINT [FK_AtomPayloadStore_Atoms_AtomId] FOREIGN KEY ([AtomId]) REFERENCES [dbo].[Atoms] ([AtomId]) ON DELETE CASCADE
);

-- =============================================
-- Table: dbo.TensorAtomPayloads
-- =============================================
-- Stores the raw binary payload for a single tensor atom component.
-- This typically represents a vector (e.g., a row from the V matrix in SVD)
-- that is part of a larger decomposed model layer.
--
-- This table uses FILESTREAM to efficiently store the VARBINARY data
-- outside of the main database file, enabling fast, transactional,
-- stream-based access from the SQL CLR.
-- =============================================
CREATE TABLE [dbo].[TensorAtomPayloads]
(
    [PayloadId] BIGINT IDENTITY(1,1) NOT NULL,
    [TensorAtomId] BIGINT NOT NULL,
    [RowGuid] UNIQUEIDENTIFIER ROWGUIDCOL NOT NULL DEFAULT (NEWID()),
    [Payload] VARBINARY(MAX) FILESTREAM NULL,
    [Metadata] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_TensorAtomPayloads] PRIMARY KEY CLUSTERED ([PayloadId]),
    CONSTRAINT [UQ_TensorAtomPayloads_RowGuid] UNIQUE NONCLUSTERED ([RowGuid]),
    CONSTRAINT [FK_TensorAtomPayloads_TensorAtoms] FOREIGN KEY ([TensorAtomId])
        REFERENCES [dbo].[TensorAtoms]([TensorAtomId])
        ON DELETE CASCADE
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_TensorAtomPayloads_TensorAtomId]
    ON [dbo].[TensorAtomPayloads]([TensorAtomId]);
GO
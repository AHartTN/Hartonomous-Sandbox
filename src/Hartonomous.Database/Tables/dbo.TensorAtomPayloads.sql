CREATE TABLE dbo.TensorAtomPayloads (
    PayloadId BIGINT IDENTITY(1,1) NOT NULL,
    TensorAtomId BIGINT NOT NULL,
    RowGuid UNIQUEIDENTIFIER ROWGUIDCOL NOT NULL DEFAULT NEWID(),
    Payload VARBINARY(MAX) FILESTREAM NULL,
    Metadata NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_TensorAtomPayloads PRIMARY KEY CLUSTERED (PayloadId),
    CONSTRAINT UQ_TensorAtomPayloads_RowGuid UNIQUE (RowGuid),
    CONSTRAINT FK_TensorAtomPayloads_TensorAtom FOREIGN KEY (TensorAtomId) REFERENCES dbo.TensorAtom(TensorAtomId) ON DELETE CASCADE,
    INDEX IX_TensorAtomPayloads_TensorAtomId UNIQUE (TensorAtomId)
) FILESTREAM_ON HartonomousFileStream;
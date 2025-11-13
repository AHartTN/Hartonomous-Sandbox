CREATE TABLE dbo.TensorAtomPayloads (
    PayloadId BIGINT IDENTITY(1,1) NOT NULL,
    TensorAtomId BIGINT NOT NULL,
    RowGuid UNIQUEIDENTIFIER ROWGUIDCOL NOT NULL DEFAULT NEWID(),
    Payload VARBINARY(MAX) FILESTREAM NULL,
    Metadata NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_TensorAtomPayloads PRIMARY KEY CLUSTERED (PayloadId),
    CONSTRAINT UQ_TensorAtomPayloads_RowGuid UNIQUE (RowGuid),
    CONSTRAINT FK_TensorAtomPayloads_TensorAtoms FOREIGN KEY (TensorAtomId) REFERENCES dbo.TensorAtoms(TensorAtomId) ON DELETE CASCADE,
    INDEX IX_TensorAtomPayloads_TensorAtomId UNIQUE (TensorAtomId)
) ON [PRIMARY];
CREATE TABLE provenance.GenerationStreams (
    GenerationStreamId BIGINT IDENTITY(1,1) NOT NULL,
    ModelId INT NOT NULL,
    GeneratedAtomIds NVARCHAR(MAX) NOT NULL,
    ProvenanceStream provenance.AtomicStream NOT NULL,
    ContextMetadata JSON,
    TenantId INT NOT NULL DEFAULT 0,
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_GenerationStreams PRIMARY KEY (GenerationStreamId),
    CONSTRAINT FK_GenerationStreams_Models FOREIGN KEY (ModelId) REFERENCES dbo.Models(ModelId),
    INDEX IX_GenerationStreams_ModelId (ModelId),
    INDEX IX_GenerationStreams_TenantId (TenantId),
    INDEX IX_GenerationStreams_CreatedUtc (CreatedUtc)
);

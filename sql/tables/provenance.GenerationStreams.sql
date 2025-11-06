-- GenerationStreams table for storing inference provenance
-- Each row represents one generation operation with complete AtomicStream tracking

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'provenance')
BEGIN
    EXEC('CREATE SCHEMA provenance');
END
GO

CREATE TABLE provenance.GenerationStreams (
    GenerationStreamId BIGINT IDENTITY(1,1) PRIMARY KEY,
    
    -- Model used for generation
    ModelId INT NOT NULL,
    
    -- Generated atoms (comma-separated IDs)
    GeneratedAtomIds NVARCHAR(MAX) NOT NULL,
    
    -- Complete provenance: AtomicStream with all input/generated segments
    ProvenanceStream provenance.AtomicStream NOT NULL,
    
    -- Context metadata (JSON)
    ContextMetadata NVARCHAR(MAX),
    
    -- Tenant isolation
    TenantId INT NOT NULL DEFAULT 0,
    
    -- Timestamps
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    -- Indexes
    INDEX IX_GenerationStreams_ModelId (ModelId),
    INDEX IX_GenerationStreams_TenantId (TenantId),
    INDEX IX_GenerationStreams_CreatedUtc (CreatedUtc)
);
GO

-- Foreign key to Models
ALTER TABLE provenance.GenerationStreams
ADD CONSTRAINT FK_GenerationStreams_Models
FOREIGN KEY (ModelId) REFERENCES dbo.Models(ModelId);
GO

-- CHECK constraint for valid JSON
ALTER TABLE provenance.GenerationStreams
ADD CONSTRAINT CK_GenerationStreams_ContextMetadata
CHECK (ContextMetadata IS NULL OR ISJSON(ContextMetadata) = 1);
GO

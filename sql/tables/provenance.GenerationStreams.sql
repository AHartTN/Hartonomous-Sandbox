-- =============================================
-- Table: provenance.GenerationStreams
-- =============================================
-- Represents a generation operation with complete provenance tracking.
-- This table was previously managed by EF Core.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'provenance')
BEGIN
    EXEC('CREATE SCHEMA provenance');
END
GO

IF OBJECT_ID('provenance.GenerationStreams', 'U') IS NOT NULL
    DROP TABLE provenance.GenerationStreams;
GO

CREATE TABLE provenance.GenerationStreams
(
    StreamId            UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    GenerationStreamId  BIGINT           IDENTITY(1,1) NOT NULL,
    ModelId             INT              NULL,
    Scope               NVARCHAR(128)    NULL,
    Model               NVARCHAR(128)    NULL,
    GeneratedAtomIds    NVARCHAR(MAX)    NULL,
    ProvenanceStream    VARBINARY(MAX)   NULL,
    ContextMetadata     NVARCHAR(MAX)    NULL,
    TenantId            INT              NOT NULL DEFAULT 0,
    CreatedUtc          DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_GenerationStreams_Models FOREIGN KEY (ModelId) REFERENCES dbo.Models(ModelId) ON DELETE NO ACTION,
    CONSTRAINT CK_GenerationStreams_ContextMetadata_IsJson CHECK (ContextMetadata IS NULL OR ISJSON(ContextMetadata) = 1)
);
GO

CREATE INDEX IX_GenerationStreams_GenerationStreamId ON provenance.GenerationStreams(GenerationStreamId);
GO

CREATE INDEX IX_GenerationStreams_Scope ON provenance.GenerationStreams(Scope);
GO

CREATE INDEX IX_GenerationStreams_Model ON provenance.GenerationStreams(Model);
GO

CREATE INDEX IX_GenerationStreams_ModelId ON provenance.GenerationStreams(ModelId);
GO

CREATE INDEX IX_GenerationStreams_TenantId ON provenance.GenerationStreams(TenantId);
GO

CREATE INDEX IX_GenerationStreams_CreatedUtc ON provenance.GenerationStreams(CreatedUtc);
GO

PRINT 'Created table provenance.GenerationStreams';
GO
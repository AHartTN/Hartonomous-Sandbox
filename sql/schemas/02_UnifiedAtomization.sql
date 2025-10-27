-- =============================================
-- Hartonomous Unified Atomization Schema
-- =============================================

USE Hartonomous;
GO

-- =============================================
-- File Ingestion Tables
-- =============================================

-- Files: A central registry for all ingested files
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Files]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.Files (
        file_id BIGINT PRIMARY KEY IDENTITY(1,1),
        source_path NVARCHAR(4000) NOT NULL,
        file_hash BINARY(32) NOT NULL, -- SHA256 hash of the entire file
        file_size_bytes BIGINT NOT NULL,
        file_type NVARCHAR(100), -- e.g., 'image/jpeg', 'application/pdf', 'text/plain'
        ingestion_date DATETIME2 DEFAULT SYSUTCDATETIME(),
        metadata JSON -- FIXED: Native JSON for file metadata
    );
    PRINT 'Table dbo.Files created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_file_hash' AND object_id = OBJECT_ID('dbo.Files'))
BEGIN
    CREATE UNIQUE INDEX idx_file_hash ON dbo.Files(file_hash);
    PRINT 'Index idx_file_hash on dbo.Files created.';
END
GO

-- Chunks: Content-addressable storage for binary data chunks
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Chunks]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.Chunks (
        chunk_hash BINARY(32) PRIMARY KEY, -- SHA256 hash of the chunk data
        chunk_data VARBINARY(MAX) NOT NULL, -- JUSTIFIED: Raw binary chunks, no structured type available
        chunk_size_bytes INT NOT NULL,
        first_seen DATETIME2 DEFAULT SYSUTCDATETIME()
    );
    PRINT 'Table dbo.Chunks created.';
END
GO

-- Atoms: The core of the unified atomization schema
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Atoms]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.Atoms (
        atom_id BIGINT PRIMARY KEY IDENTITY(1,1),
        file_id BIGINT NOT NULL,
        parent_atom_id BIGINT,
        atom_type NVARCHAR(100) NOT NULL, -- e.g., 'paragraph', 'sentence', 'word', 'image_patch', 'audio_frame'
        
        -- Data columns for different modalities
        text_value NVARCHAR(MAX), -- JUSTIFIED: Free-form text content, no structure
        numeric_value FLOAT,
        datetime_value DATETIME2,
        spatial_value GEOMETRY,
        vector_value VECTOR(1536), -- FIXED: Use native VECTOR type (max 1536 for flexibility)
        chunk_hash BINARY(32), -- For binary data

        -- Metadata
        metadata JSON, -- FIXED: Native JSON for atom-specific metadata
        
        FOREIGN KEY (file_id) REFERENCES dbo.Files(file_id) ON DELETE CASCADE,
        FOREIGN KEY (parent_atom_id) REFERENCES dbo.Atoms(atom_id),
        FOREIGN KEY (chunk_hash) REFERENCES dbo.Chunks(chunk_hash)
    );
    PRINT 'Table dbo.Atoms created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_atom_file_id' AND object_id = OBJECT_ID('dbo.Atoms'))
BEGIN
    CREATE INDEX idx_atom_file_id ON dbo.Atoms(file_id);
    PRINT 'Index idx_atom_file_id on dbo.Atoms created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_atom_type' AND object_id = OBJECT_ID('dbo.Atoms'))
BEGIN
    CREATE INDEX idx_atom_type ON dbo.Atoms(atom_type);
    PRINT 'Index idx_atom_type on dbo.Atoms created.';
END
GO

-- Relationships: Defines relationships between atoms
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Relationships]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.Relationships (
        relationship_id BIGINT PRIMARY KEY IDENTITY(1,1),
        source_atom_id BIGINT NOT NULL,
        target_atom_id BIGINT NOT NULL,
        relationship_type NVARCHAR(100) NOT NULL, -- e.g., 'contains', 'precedes', 'is_related_to'
        weight FLOAT,
        metadata JSON, -- FIXED: Native JSON for relationship metadata

        FOREIGN KEY (source_atom_id) REFERENCES dbo.Atoms(atom_id) ON DELETE NO ACTION, -- NO ACTION to avoid cycles
        FOREIGN KEY (target_atom_id) REFERENCES dbo.Atoms(atom_id) ON DELETE NO ACTION
    );
    PRINT 'Table dbo.Relationships created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_relationship_source' AND object_id = OBJECT_ID('dbo.Relationships'))
BEGIN
    CREATE INDEX idx_relationship_source ON dbo.Relationships(source_atom_id);
    PRINT 'Index idx_relationship_source on dbo.Relationships created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_relationship_target' AND object_id = OBJECT_ID('dbo.Relationships'))
BEGIN
    CREATE INDEX idx_relationship_target ON dbo.Relationships(target_atom_id);
    PRINT 'Index idx_relationship_target on dbo.Relationships created.';
END
GO

PRINT 'Unified Atomization schema script completed.';
GO
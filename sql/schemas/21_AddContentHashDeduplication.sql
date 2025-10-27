-- =============================================
-- Add Content Hash for Deduplication
-- Atomic component storage: never store the same thing twice
-- =============================================

USE Hartonomous;
GO

-- Add content_hash column to Embeddings_Production
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Embeddings_Production') 
    AND name = 'content_hash'
)
BEGIN
    ALTER TABLE dbo.Embeddings_Production
    ADD content_hash BINARY(32) NULL; -- SHA256 hash of source_text
    
    PRINT 'Added content_hash column to Embeddings_Production';
END
GO

-- Add last_accessed column for deduplication tracking
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Embeddings_Production') 
    AND name = 'last_accessed'
)
BEGIN
    ALTER TABLE dbo.Embeddings_Production
    ADD last_accessed DATETIME2 NULL;
    
    PRINT 'Added last_accessed column to Embeddings_Production';
END
GO

-- Create index on content_hash for fast duplicate detection
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'idx_content_hash' 
    AND object_id = OBJECT_ID('dbo.Embeddings_Production')
)
BEGIN
    CREATE INDEX idx_content_hash ON dbo.Embeddings_Production(content_hash)
    WHERE content_hash IS NOT NULL;
    
    PRINT 'Created index idx_content_hash on Embeddings_Production';
END
GO

-- Add content_hash column to Embeddings_Staging
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Embeddings_Staging') 
    AND name = 'content_hash'
)
BEGIN
    ALTER TABLE dbo.Embeddings_Staging
    ADD content_hash BINARY(32) NULL;
    
    PRINT 'Added content_hash column to Embeddings_Staging';
END
GO

-- Add content_hash column to Embeddings_DiskANN
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Embeddings_DiskANN') 
    AND name = 'content_hash'
)
BEGIN
    ALTER TABLE dbo.Embeddings_DiskANN
    ADD content_hash BINARY(32) NULL;
    
    PRINT 'Added content_hash column to Embeddings_DiskANN';
END
GO

-- =============================================
-- Atomic Component Storage Tables
-- Content-addressable storage for true deduplication
-- =============================================

-- AtomicPixels: Store unique pixel values ONCE
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AtomicPixels]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.AtomicPixels (
        pixel_hash BINARY(32) PRIMARY KEY, -- SHA256(r,g,b,a)
        r TINYINT NOT NULL,
        g TINYINT NOT NULL,
        b TINYINT NOT NULL,
        a TINYINT NOT NULL DEFAULT 255,
        
        -- Spatial representation in color space
        color_point GEOMETRY, -- POINT(r, g, b) in RGB color space
        
        -- Usage tracking
        reference_count BIGINT DEFAULT 0,
        first_seen DATETIME2 DEFAULT SYSUTCDATETIME(),
        last_referenced DATETIME2 DEFAULT SYSUTCDATETIME()
    );
    
    PRINT 'Table dbo.AtomicPixels created';
END
GO

CREATE SPATIAL INDEX idx_color_space ON dbo.AtomicPixels(color_point)
WITH (BOUNDING_BOX = (0, 0, 255, 255));
GO

-- AtomicAudioSamples: Store unique audio amplitude values ONCE
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AtomicAudioSamples]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.AtomicAudioSamples (
        sample_hash BINARY(32) PRIMARY KEY, -- SHA256(amplitude)
        amplitude_normalized FLOAT NOT NULL, -- -1.0 to 1.0
        amplitude_int16 SMALLINT NOT NULL, -- Raw int16 value
        
        -- Usage tracking
        reference_count BIGINT DEFAULT 0,
        first_seen DATETIME2 DEFAULT SYSUTCDATETIME(),
        last_referenced DATETIME2 DEFAULT SYSUTCDATETIME()
    );
    
    PRINT 'Table dbo.AtomicAudioSamples created';
END
GO

CREATE INDEX idx_amplitude ON dbo.AtomicAudioSamples(amplitude_normalized);
GO

-- AtomicVectorComponents: Store unique vector component values ONCE
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AtomicVectorComponents]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.AtomicVectorComponents (
        component_hash BINARY(32) PRIMARY KEY, -- SHA256(float_value)
        float_value FLOAT NOT NULL,
        
        -- Quantized representations
        float16_bytes BINARY(2), -- Half-precision storage
        int8_quantized TINYINT, -- 8-bit quantization
        
        -- Usage tracking
        reference_count BIGINT DEFAULT 0,
        first_seen DATETIME2 DEFAULT SYSUTCDATETIME(),
        last_referenced DATETIME2 DEFAULT SYSUTCDATETIME()
    );
    
    PRINT 'Table dbo.AtomicVectorComponents created';
END
GO

CREATE UNIQUE INDEX idx_float_value ON dbo.AtomicVectorComponents(float_value);
GO

-- AtomicTextTokens: Store unique tokens ONCE
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AtomicTextTokens]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.AtomicTextTokens (
        token_hash BINARY(32) PRIMARY KEY, -- SHA256(token_text)
        token_text NVARCHAR(200) NOT NULL,
        token_length INT NOT NULL,
        
        -- Token embedding (VECTOR type for semantic deduplication)
        token_embedding VECTOR(768), -- Native SQL Server 2025 VECTOR type
        embedding_model NVARCHAR(100),
        
        -- Usage tracking
        reference_count BIGINT DEFAULT 0,
        first_seen DATETIME2 DEFAULT SYSUTCDATETIME(),
        last_referenced DATETIME2 DEFAULT SYSUTCDATETIME()
    );
    
    PRINT 'Table dbo.AtomicTextTokens created';
END
GO

CREATE UNIQUE INDEX idx_token_text ON dbo.AtomicTextTokens(token_text);
GO

-- AtomicWaveformPatterns: Store unique short waveform patterns ONCE
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AtomicWaveformPatterns]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.AtomicWaveformPatterns (
        pattern_hash BINARY(32) PRIMARY KEY, -- SHA256(sample sequence)
        pattern_data VARBINARY(MAX) NOT NULL, -- JUSTIFIED: Raw audio sample bytes, no structure
        pattern_length INT NOT NULL,
        
        -- Waveform as spatial geometry
        waveform_geometry GEOMETRY,
        
        -- Pattern features
        rms_energy FLOAT,
        zero_crossing_rate FLOAT,
        spectral_centroid FLOAT,
        
        -- Usage tracking
        reference_count BIGINT DEFAULT 0,
        first_seen DATETIME2 DEFAULT SYSUTCDATETIME(),
        last_referenced DATETIME2 DEFAULT SYSUTCDATETIME()
    );
    
    PRINT 'Table dbo.AtomicWaveformPatterns created';
END
GO

CREATE SPATIAL INDEX idx_waveform ON dbo.AtomicWaveformPatterns(waveform_geometry)
WITH (BOUNDING_BOX = (0, -1, 1000, 1));
GO

-- =============================================
-- Atomic References (Map full data to atomic components)
-- =============================================

-- ImagePixelReferences: Map image pixels to atomic pixel values
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ImagePixelReferences]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.ImagePixelReferences (
        image_id BIGINT NOT NULL,
        pixel_x INT NOT NULL,
        pixel_y INT NOT NULL,
        pixel_hash BINARY(32) NOT NULL, -- References AtomicPixels
        
        PRIMARY KEY (image_id, pixel_x, pixel_y),
        FOREIGN KEY (image_id) REFERENCES dbo.Images(image_id) ON DELETE CASCADE,
        FOREIGN KEY (pixel_hash) REFERENCES dbo.AtomicPixels(pixel_hash)
    );
    
    -- COLUMNSTORE for extreme compression (images have MILLIONS of pixels)
    CREATE CLUSTERED COLUMNSTORE INDEX idx_pixel_refs ON dbo.ImagePixelReferences;
    
    PRINT 'Table dbo.ImagePixelReferences created with COLUMNSTORE index';
END
GO

-- AudioSampleReferences: Map audio frames to atomic samples
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AudioSampleReferences]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.AudioSampleReferences (
        audio_id BIGINT NOT NULL,
        sample_number BIGINT NOT NULL,
        channel TINYINT NOT NULL, -- 0 = left, 1 = right, etc.
        sample_hash BINARY(32) NOT NULL, -- References AtomicAudioSamples
        
        PRIMARY KEY (audio_id, sample_number, channel),
        FOREIGN KEY (audio_id) REFERENCES dbo.AudioData(audio_id) ON DELETE CASCADE,
        FOREIGN KEY (sample_hash) REFERENCES dbo.AtomicAudioSamples(sample_hash)
    );
    
    -- COLUMNSTORE for extreme compression (audio has MILLIONS of samples)
    CREATE CLUSTERED COLUMNSTORE INDEX idx_audio_refs ON dbo.AudioSampleReferences;
    
    PRINT 'Table dbo.AudioSampleReferences created with COLUMNSTORE index';
END
GO

PRINT 'Content-addressable atomic storage schema completed';
PRINT 'Deduplication enabled at atomic component level';
GO

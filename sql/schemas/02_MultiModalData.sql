-- =============================================
-- Hartonomous Multi-Modal Data Storage
-- Exploiting SQL Server 2025 datatypes for AI
-- =============================================

USE Hartonomous;
GO

-- =============================================
-- IMAGE STORAGE (Multiple Representations)
-- =============================================

-- Images: Primary image storage with multiple representations
CREATE TABLE dbo.Images (
    image_id BIGINT PRIMARY KEY IDENTITY(1,1),
    source_path NVARCHAR(500),
    source_url NVARCHAR(1000),

    -- Raw data
    raw_data VARBINARY(MAX), -- JUSTIFIED: Encoded image bytes (PNG/JPG), no structured type
    width INT NOT NULL,
    height INT NOT NULL,
    channels INT NOT NULL, -- 1 (grayscale), 3 (RGB), 4 (RGBA)
    format NVARCHAR(20), -- 'png', 'jpg', 'bmp', etc.

    -- Spatial representations (pixels as geometry)
    pixel_cloud GEOMETRY, -- MULTIPOINT: representative pixels
    edge_map GEOMETRY, -- LINESTRING: detected edges
    object_regions GEOMETRY, -- MULTIPOLYGON: segmented objects
    saliency_regions GEOMETRY, -- POLYGON: attention regions

    -- Vector representations
    global_embedding VECTOR(1536), -- FIXED: Use native VECTOR type (CLIP, DINOv2 embeddings)
    global_embedding_dim INT,
    -- NOTE: Individual patch embeddings stored in ImagePatches table, not aggregated here

    -- Metadata (JSON)
    metadata JSON, -- FIXED: Native JSON for EXIF, labels, captions

    ingestion_date DATETIME2 DEFAULT SYSUTCDATETIME(),
    last_accessed DATETIME2,
    access_count BIGINT DEFAULT 0,

    INDEX idx_dimensions (width, height),
    INDEX idx_ingestion (ingestion_date DESC)
);
GO

-- Create spatial indexes (4-level hierarchy = multi-resolution)
CREATE SPATIAL INDEX idx_pixel_cloud ON dbo.Images(pixel_cloud)
WITH (
    BOUNDING_BOX = (0, 0, 10000, 10000),
    GRIDS = (LEVEL_1 = HIGH, LEVEL_2 = HIGH, LEVEL_3 = MEDIUM, LEVEL_4 = LOW)
);
GO

CREATE SPATIAL INDEX idx_object_regions ON dbo.Images(object_regions)
WITH (
    BOUNDING_BOX = (0, 0, 10000, 10000),
    GRIDS = (LEVEL_1 = HIGH, LEVEL_2 = HIGH, LEVEL_3 = MEDIUM, LEVEL_4 = LOW)
);
GO

-- ImagePatches: Fine-grained patch-level data for detailed analysis
CREATE TABLE dbo.ImagePatches (
    patch_id BIGINT PRIMARY KEY IDENTITY(1,1),
    image_id BIGINT NOT NULL,
    patch_x INT NOT NULL, -- Top-left corner
    patch_y INT NOT NULL,
    patch_width INT NOT NULL,
    patch_height INT NOT NULL,

    -- Patch as spatial region
    patch_region GEOMETRY NOT NULL,

    -- Patch features
    patch_embedding VECTOR(768), -- FIXED: Native VECTOR type for patch embeddings
    dominant_color GEOMETRY, -- POINT(r, g, b) in color space
    texture_features VARBINARY(MAX), -- JUSTIFIED: Binary texture descriptors

    -- Statistics
    mean_intensity FLOAT,
    std_intensity FLOAT,

    FOREIGN KEY (image_id) REFERENCES dbo.Images(image_id) ON DELETE CASCADE,
    INDEX idx_image_patches (image_id, patch_x, patch_y)
);
GO

CREATE SPATIAL INDEX idx_patch_spatial ON dbo.ImagePatches(patch_region)
WITH (BOUNDING_BOX = (0, 0, 10000, 10000));
GO

-- =============================================
-- AUDIO STORAGE
-- =============================================

-- AudioData: Primary audio storage
CREATE TABLE dbo.AudioData (
    audio_id BIGINT PRIMARY KEY IDENTITY(1,1),
    source_path NVARCHAR(500),

    -- Raw data
    raw_data VARBINARY(MAX), -- JUSTIFIED: Encoded audio bytes (WAV/MP3), no structured type
    sample_rate INT NOT NULL,
    duration_ms BIGINT NOT NULL,
    num_channels TINYINT NOT NULL, -- 1 (mono), 2 (stereo), etc.
    format NVARCHAR(20), -- 'wav', 'mp3', 'flac', etc.

    -- Spectral representations (as spatial data)
    spectrogram GEOMETRY, -- 2D heatmap: time × frequency
    mel_spectrogram GEOMETRY,

    -- Waveforms as geometry (for similarity matching)
    waveform_left GEOMETRY, -- LINESTRING
    waveform_right GEOMETRY,

    -- Vector representations
    global_embedding VECTOR(768), -- FIXED: Native VECTOR type (Wav2Vec2, HuBERT)
    global_embedding_dim INT,

    -- Metadata
    metadata JSON, -- FIXED: Native JSON for genre, artist, BPM, key

    ingestion_date DATETIME2 DEFAULT SYSUTCDATETIME(),
    INDEX idx_duration (duration_ms),
    INDEX idx_ingestion (ingestion_date DESC)
);
GO

-- AudioFrames: Frame-by-frame temporal data (COLUMNSTORE for compression!)
CREATE TABLE dbo.AudioFrames (
    audio_id BIGINT NOT NULL,
    frame_number BIGINT NOT NULL,
    timestamp_ms BIGINT NOT NULL,

    -- Amplitude data
    amplitude_l FLOAT,
    amplitude_r FLOAT,

    -- Spectral features per frame
    spectral_centroid FLOAT,
    spectral_rolloff FLOAT,
    zero_crossing_rate FLOAT,
    rms_energy FLOAT,

    -- MFCC features
    mfcc VECTOR(13), -- FIXED: 13-dimensional MFCC as native VECTOR

    -- Frame embedding
    frame_embedding VECTOR(768), -- FIXED: Native VECTOR per frame

    PRIMARY KEY (audio_id, frame_number),
    FOREIGN KEY (audio_id) REFERENCES dbo.AudioData(audio_id) ON DELETE CASCADE
);
GO

-- COLUMNSTORE index for extreme compression of temporal data
CREATE CLUSTERED COLUMNSTORE INDEX idx_audio_temporal ON dbo.AudioFrames;
GO

-- =============================================
-- VIDEO STORAGE
-- =============================================

-- Videos: Primary video storage
CREATE TABLE dbo.Videos (
    video_id BIGINT PRIMARY KEY IDENTITY(1,1),
    source_path NVARCHAR(500),

    -- Raw data
    raw_data VARBINARY(MAX), -- JUSTIFIED: Encoded video bytes, no structured type
    fps INT NOT NULL,
    duration_ms BIGINT NOT NULL,
    resolution_width INT NOT NULL,
    resolution_height INT NOT NULL,
    num_frames BIGINT NOT NULL,
    format NVARCHAR(20),

    -- Global representation
    global_embedding VECTOR(768), -- FIXED: Native VECTOR (VideoMAE, TimeSformer)
    global_embedding_dim INT,

    -- Metadata
    metadata JSON, -- FIXED: Native JSON for video metadata

    ingestion_date DATETIME2 DEFAULT SYSUTCDATETIME(),
    INDEX idx_resolution (resolution_width, resolution_height),
    INDEX idx_ingestion (ingestion_date DESC)
);
GO

-- VideoFrames: Per-frame spatial data (like images)
CREATE TABLE dbo.VideoFrames (
    frame_id BIGINT PRIMARY KEY IDENTITY(1,1),
    video_id BIGINT NOT NULL,
    frame_number BIGINT NOT NULL,
    timestamp_ms BIGINT NOT NULL,

    -- Frame as spatial data (like ImagePatches)
    pixel_cloud GEOMETRY,
    object_regions GEOMETRY,

    -- Motion information
    motion_vectors GEOMETRY, -- MULTILINESTRING showing pixel movement
    optical_flow GEOMETRY, -- Vector field representation

    -- Frame embedding
    frame_embedding VECTOR(768), -- FIXED: Native VECTOR for video frames

    -- Frame similarity hash (for deduplication)
    perceptual_hash BINARY(8),

    FOREIGN KEY (video_id) REFERENCES dbo.Videos(video_id) ON DELETE CASCADE,
    UNIQUE INDEX idx_video_frame (video_id, frame_number),
    INDEX idx_timestamp (video_id, timestamp_ms)
);
GO

-- COLUMNSTORE for temporal compression
CREATE COLUMNSTORE INDEX idx_video_temporal ON dbo.VideoFrames(video_id, frame_number, timestamp_ms);
GO

CREATE SPATIAL INDEX idx_frame_objects ON dbo.VideoFrames(object_regions)
WITH (BOUNDING_BOX = (0, 0, 10000, 10000));
GO

-- =============================================
-- TEXT STORAGE
-- =============================================

-- TextDocuments: Primary text storage
CREATE TABLE dbo.TextDocuments (
    doc_id BIGINT PRIMARY KEY IDENTITY(1,1),
    source_path NVARCHAR(500),
    source_url NVARCHAR(1000),

    -- Text content
    raw_text NVARCHAR(MAX) NOT NULL, -- JUSTIFIED: Free-form text, no structure
    language NVARCHAR(10), -- 'en', 'es', 'fr', etc.
    char_count INT,
    word_count INT,

    -- Document embedding
    doc_embedding VECTOR(768), -- FIXED: Native VECTOR (Sentence-BERT, BGE)
    doc_embedding_dim INT,

    -- Metadata
    metadata JSON, -- FIXED: Native JSON for title, author, source

    ingestion_date DATETIME2 DEFAULT SYSUTCDATETIME(),
    last_accessed DATETIME2,
    access_count BIGINT DEFAULT 0,

    INDEX idx_language (language),
    INDEX idx_ingestion (ingestion_date DESC)
);
GO

-- Full-text index for keyword search (combine with vector search!)
CREATE FULLTEXT INDEX ON dbo.TextDocuments(raw_text)
KEY INDEX PK__TextDocu__CFC05D3D1234ABCD; -- Adjust key name as needed
GO

-- TextTokens: Token-level data for detailed analysis
CREATE TABLE dbo.TextTokens (
    token_seq_id BIGINT PRIMARY KEY IDENTITY(1,1),
    doc_id BIGINT NOT NULL,
    token_position INT NOT NULL,

    token_text NVARCHAR(100) NOT NULL,
    token_id INT, -- From tokenizer vocabulary

    -- Token embedding
    token_embedding VECTOR(768), -- FIXED: Native VECTOR type

    -- Attention mask / special token flag
    attention_mask BIT DEFAULT 1,
    is_special BIT DEFAULT 0, -- [CLS], [SEP], <PAD>, etc.

    FOREIGN KEY (doc_id) REFERENCES dbo.TextDocuments(doc_id) ON DELETE CASCADE,
    UNIQUE INDEX idx_doc_token (doc_id, token_position)
);
GO

-- COLUMNSTORE for efficient token sequence storage
CREATE COLUMNSTORE INDEX idx_token_sequence ON dbo.TextTokens;
GO

-- =============================================
-- CROSS-MODAL RELATIONSHIPS
-- =============================================

-- MultiModalRelations: Link different modalities
CREATE TABLE dbo.MultiModalRelations (
    relation_id BIGINT PRIMARY KEY IDENTITY(1,1),

    -- Source modality
    source_type NVARCHAR(20) NOT NULL, -- 'image', 'audio', 'video', 'text'
    source_id BIGINT NOT NULL,

    -- Target modality
    target_type NVARCHAR(20) NOT NULL,
    target_id BIGINT NOT NULL,

    -- Relationship
    relation_type NVARCHAR(50), -- 'caption', 'transcription', 'thumbnail', 'soundtrack', etc.
    confidence FLOAT DEFAULT 1.0,

    -- Metadata
    metadata JSON, -- FIXED: Native JSON for relation metadata

    created_date DATETIME2 DEFAULT SYSUTCDATETIME(),

    INDEX idx_source (source_type, source_id),
    INDEX idx_target (target_type, target_id),
    INDEX idx_relation (relation_type)
);
GO

-- =============================================
-- TIME SERIES / SEQUENTIAL DATA
-- =============================================

-- TimeSeriesData: Generic time-series storage
CREATE TABLE dbo.TimeSeriesData (
    series_id BIGINT NOT NULL,
    timestamp_ms BIGINT NOT NULL,
    sequence_num BIGINT NOT NULL,

    -- Values
    value FLOAT,
    value_vector VECTOR(256), -- FIXED: Multi-dimensional time series values as VECTOR

    -- Pattern embedding
    pattern_embedding VECTOR(128), -- FIXED: Local pattern embeddings as VECTOR

    -- Metadata
    tags JSON, -- FIXED: Native JSON for flexible tagging

    PRIMARY KEY (series_id, timestamp_ms)
);
GO

-- COLUMNSTORE for 10-100x compression!
CREATE CLUSTERED COLUMNSTORE INDEX idx_timeseries ON dbo.TimeSeriesData;
GO

-- =============================================
-- GENERATED OUTPUT STORAGE
-- =============================================

-- GeneratedImages: Store AI-generated images
CREATE TABLE dbo.GeneratedImages (
    gen_image_id BIGINT PRIMARY KEY IDENTITY(1,1),
    inference_id BIGINT, -- Link to inference request

    prompt NVARCHAR(MAX), -- JUSTIFIED: Free-form prompt text
    negative_prompt NVARCHAR(MAX), -- JUSTIFIED: Free-form negative prompt
    models_used NVARCHAR(500), -- Comma-separated model IDs

    -- Generated data
    latent_vector VECTOR(1024), -- FIXED: Latent space as VECTOR (Stable Diffusion, DALL-E)
    pixel_data GEOMETRY, -- Generated pixels as spatial data
    final_image VARBINARY(MAX), -- JUSTIFIED: Encoded PNG/JPG bytes

    -- Metadata
    width INT,
    height INT,
    generation_steps INT,
    guidance_scale FLOAT,
    seed BIGINT,

    generation_date DATETIME2 DEFAULT SYSUTCDATETIME(),
    duration_ms INT,

    -- User feedback
    user_rating TINYINT,

    FOREIGN KEY (inference_id) REFERENCES dbo.InferenceRequests(inference_id),
    INDEX idx_generation_date (generation_date DESC)
);
GO

-- GeneratedAudio: Store AI-generated audio
CREATE TABLE dbo.GeneratedAudio (
    gen_audio_id BIGINT PRIMARY KEY IDENTITY(1,1),
    inference_id BIGINT,

    prompt NVARCHAR(MAX), -- JUSTIFIED: Free-form prompt
    models_used NVARCHAR(500),

    -- Generated data
    waveform GEOMETRY, -- As LINESTRING
    audio_data VARBINARY(MAX), -- JUSTIFIED: Encoded WAV/MP3 bytes

    -- Metadata
    sample_rate INT,
    duration_ms INT,
    num_channels TINYINT,

    generation_date DATETIME2 DEFAULT SYSUTCDATETIME(),
    duration_ms INT,

    -- User feedback
    user_rating TINYINT,

    FOREIGN KEY (inference_id) REFERENCES dbo.InferenceRequests(inference_id),
    INDEX idx_generation_date (generation_date DESC)
);
GO

-- GeneratedText: Store AI-generated text
CREATE TABLE dbo.GeneratedText (
    gen_text_id BIGINT PRIMARY KEY IDENTITY(1,1),
    inference_id BIGINT,

    prompt NVARCHAR(MAX), -- JUSTIFIED: Free-form prompt
    models_used NVARCHAR(500),

    -- Generated text
    generated_text NVARCHAR(MAX), -- JUSTIFIED: Free-form generated text
    token_count INT,

    -- Metadata
    temperature FLOAT,
    top_p FLOAT,
    max_tokens INT,

    generation_date DATETIME2 DEFAULT SYSUTCDATETIME(),
    duration_ms INT,

    -- User feedback
    user_rating TINYINT,

    FOREIGN KEY (inference_id) REFERENCES dbo.InferenceRequests(inference_id),
    INDEX idx_generation_date (generation_date DESC)
);
GO

PRINT 'Multi-modal data tables created successfully.';
PRINT 'Next: Enable SQL Server 2025 preview features and create VECTOR indexes.';
PRINT 'Run: EXEC sp_configure ''preview features'', 1; RECONFIGURE;';
GO

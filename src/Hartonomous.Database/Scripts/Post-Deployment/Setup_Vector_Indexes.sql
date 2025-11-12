USE [$(DatabaseName)]
GO

-- Step 1: Enable PREVIEW_FEATURES at database scope
-- Required for VECTOR data type and DiskANN indexes in SQL Server 2025
IF NOT EXISTS (SELECT 1 FROM sys.database_scoped_configurations WHERE name = 'PREVIEW_FEATURES' AND value = 1)
BEGIN
    ALTER DATABASE SCOPED CONFIGURATION SET PREVIEW_FEATURES = ON;
    PRINT 'PREVIEW_FEATURES enabled successfully.';
END
ELSE
BEGIN
    PRINT 'PREVIEW_FEATURES already enabled.';
END
GO

-- Step 2: Create DiskANN vector indexes for AtomEmbedding table
-- DiskANN: Graph-based approximate nearest neighbor algorithm
-- Optimized for high-dimensional vectors with billion-scale support

-- AtomEmbedding.EmbeddingVector (VECTOR(1998))
-- Primary vector search table for semantic similarity
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_AtomEmbedding_Vector_DiskANN' 
    AND object_id = OBJECT_ID('dbo.AtomEmbedding')
)
BEGIN
    PRINT 'Creating DiskANN index on AtomEmbedding.EmbeddingVector...';
    
    CREATE INDEX IX_AtomEmbedding_Vector_DiskANN 
    ON dbo.AtomEmbedding (EmbeddingVector)
    WITH (
        -- DiskANN algorithm for approximate nearest neighbor
        ALGORITHM = 'DiskANN',
        
        -- Cosine distance metric (suitable for normalized embeddings)
        -- Options: COSINE, EUCLIDEAN, DOT_PRODUCT
        DISTANCE_METRIC = 'COSINE',
        
        -- Build parameters
        -- R: Maximum degree of graph nodes (16-64 recommended)
        -- L: Search list size during build (50-200 recommended)
        R = 32,
        L = 100,
        
        -- Online build for minimal downtime
        ONLINE = ON
    );
    
    PRINT 'DiskANN index created on AtomEmbedding.EmbeddingVector';
END
ELSE
BEGIN
    PRINT 'DiskANN index already exists on AtomEmbedding.EmbeddingVector';
END
GO

-- TokenVocabulary.Embedding (VECTOR(1998))
-- Token embedding lookup for BPE tokenization
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_TokenVocabulary_Embedding_DiskANN' 
    AND object_id = OBJECT_ID('dbo.TokenVocabulary')
)
BEGIN
    PRINT 'Creating DiskANN index on TokenVocabulary.Embedding...';
    
    CREATE INDEX IX_TokenVocabulary_Embedding_DiskANN 
    ON dbo.TokenVocabulary (Embedding)
    WITH (
        ALGORITHM = 'DiskANN',
        DISTANCE_METRIC = 'COSINE',
        R = 32,
        L = 100,
        ONLINE = ON
    );
    
    PRINT 'DiskANN index created on TokenVocabulary.Embedding';
END
ELSE
BEGIN
    PRINT 'DiskANN index already exists on TokenVocabulary.Embedding';
END
GO

-- TextDocument.GlobalEmbedding (VECTOR(1998))
-- Document-level semantic search
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_TextDocument_GlobalEmbedding_DiskANN' 
    AND object_id = OBJECT_ID('dbo.TextDocument')
)
BEGIN
    PRINT 'Creating DiskANN index on TextDocument.GlobalEmbedding...';
    
    CREATE INDEX IX_TextDocument_GlobalEmbedding_DiskANN 
    ON dbo.TextDocument (GlobalEmbedding)
    WITH (
        ALGORITHM = 'DiskANN',
        DISTANCE_METRIC = 'COSINE',
        R = 32,
        L = 100,
        ONLINE = ON
    );
    
    PRINT 'DiskANN index created on TextDocument.GlobalEmbedding';
END
ELSE
BEGIN
    PRINT 'DiskANN index already exists on TextDocument.GlobalEmbedding';
END
GO

-- Image.GlobalEmbedding (VECTOR(1998))
-- Image semantic search
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Image_GlobalEmbedding_DiskANN' 
    AND object_id = OBJECT_ID('dbo.Image')
)
BEGIN
    PRINT 'Creating DiskANN index on Image.GlobalEmbedding...';
    
    CREATE INDEX IX_Image_GlobalEmbedding_DiskANN 
    ON dbo.Image (GlobalEmbedding)
    WITH (
        ALGORITHM = 'DiskANN',
        DISTANCE_METRIC = 'COSINE',
        R = 32,
        L = 100,
        ONLINE = ON
    );
    
    PRINT 'DiskANN index created on Image.GlobalEmbedding';
END
ELSE
BEGIN
    PRINT 'DiskANN index already exists on Image.GlobalEmbedding';
END
GO

-- AudioData.GlobalEmbedding (VECTOR(1998))
-- Audio semantic search
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_AudioData_GlobalEmbedding_DiskANN' 
    AND object_id = OBJECT_ID('dbo.AudioData')
)
BEGIN
    PRINT 'Creating DiskANN index on AudioData.GlobalEmbedding...';
    
    CREATE INDEX IX_AudioData_GlobalEmbedding_DiskANN 
    ON dbo.AudioData (GlobalEmbedding)
    WITH (
        ALGORITHM = 'DiskANN',
        DISTANCE_METRIC = 'COSINE',
        R = 32,
        L = 100,
        ONLINE = ON
    );
    
    PRINT 'DiskANN index created on AudioData.GlobalEmbedding';
END
ELSE
BEGIN
    PRINT 'DiskANN index already exists on AudioData.GlobalEmbedding';
END
GO

-- Video.GlobalEmbedding (VECTOR(1998))
-- Video semantic search
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Video_GlobalEmbedding_DiskANN' 
    AND object_id = OBJECT_ID('dbo.Video')
)
BEGIN
    PRINT 'Creating DiskANN index on Video.GlobalEmbedding...';
    
    CREATE INDEX IX_Video_GlobalEmbedding_DiskANN 
    ON dbo.Video (GlobalEmbedding)
    WITH (
        ALGORITHM = 'DiskANN',
        DISTANCE_METRIC = 'COSINE',
        R = 32,
        L = 100,
        ONLINE = ON
    );
    
    PRINT 'DiskANN index created on Video.GlobalEmbedding';
END
ELSE
BEGIN
    PRINT 'DiskANN index already exists on Video.GlobalEmbedding';
END
GO

-- ImagePatch.PatchEmbedding (VECTOR(768))
-- Fine-grained image patch search
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_ImagePatch_PatchEmbedding_DiskANN' 
    AND object_id = OBJECT_ID('dbo.ImagePatch')
)
BEGIN
    PRINT 'Creating DiskANN index on ImagePatch.PatchEmbedding...';
    
    CREATE INDEX IX_ImagePatch_PatchEmbedding_DiskANN 
    ON dbo.ImagePatch (PatchEmbedding)
    WITH (
        ALGORITHM = 'DiskANN',
        DISTANCE_METRIC = 'COSINE',
        R = 32,
        L = 100,
        ONLINE = ON
    );
    
    PRINT 'DiskANN index created on ImagePatch.PatchEmbedding';
END
ELSE
BEGIN
    PRINT 'DiskANN index already exists on ImagePatch.PatchEmbedding';
END
GO

-- AudioFrame.FrameEmbedding (VECTOR(768))
-- Audio frame-level search
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_AudioFrame_FrameEmbedding_DiskANN' 
    AND object_id = OBJECT_ID('dbo.AudioFrame')
)
BEGIN
    PRINT 'Creating DiskANN index on AudioFrame.FrameEmbedding...';
    
    CREATE INDEX IX_AudioFrame_FrameEmbedding_DiskANN 
    ON dbo.AudioFrame (FrameEmbedding)
    WITH (
        ALGORITHM = 'DiskANN',
        DISTANCE_METRIC = 'COSINE',
        R = 32,
        L = 100,
        ONLINE = ON
    );
    
    PRINT 'DiskANN index created on AudioFrame.FrameEmbedding';
END
ELSE
BEGIN
    PRINT 'DiskANN index already exists on AudioFrame.FrameEmbedding';
END
GO

-- VideoFrame.FrameEmbedding (VECTOR(768))
-- Video frame-level search
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_VideoFrame_FrameEmbedding_DiskANN' 
    AND object_id = OBJECT_ID('dbo.VideoFrame')
)
BEGIN
    PRINT 'Creating DiskANN index on VideoFrame.FrameEmbedding...';
    
    CREATE INDEX IX_VideoFrame_FrameEmbedding_DiskANN 
    ON dbo.VideoFrame (FrameEmbedding)
    WITH (
        ALGORITHM = 'DiskANN',
        DISTANCE_METRIC = 'COSINE',
        R = 32,
        L = 100,
        ONLINE = ON
    );
    
    PRINT 'DiskANN index created on VideoFrame.FrameEmbedding';
END
ELSE
BEGIN
    PRINT 'DiskANN index already exists on VideoFrame.FrameEmbedding';
END
GO
-- STAGING + PRODUCTION PATTERN FOR DISKANN
-- Writable staging table → Read-only production table with DiskANN index
USE Hartonomous;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

PRINT '============================================================';
PRINT 'DISKANN STAGING + PRODUCTION PATTERN';
PRINT '============================================================';
GO

-- ==========================================
-- PART 1: Staging Table (Writable)
-- ==========================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Embeddings_Staging')
BEGIN
    CREATE TABLE dbo.Embeddings_Staging (
        staging_id BIGINT PRIMARY KEY IDENTITY(1,1),
        source_text NVARCHAR(MAX),
        source_type NVARCHAR(50),

        -- Full vector for exact search
        embedding_full VECTOR(768),
        embedding_model NVARCHAR(100),

        -- Spatial projection for approximate search
        spatial_proj_x FLOAT,
        spatial_proj_y FLOAT,
        spatial_proj_z FLOAT,
        spatial_geometry GEOMETRY,

        -- Metadata
        dimension INT DEFAULT 768,
        created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
        promoted_to_production BIT DEFAULT 0,
        promotion_date DATETIME2 NULL
    );

    PRINT 'Created Embeddings_Staging table (writable)';
END
GO

-- Index for promoted flag filtering
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_promoted' AND object_id = OBJECT_ID('dbo.Embeddings_Staging'))
BEGIN
    CREATE INDEX idx_promoted
    ON dbo.Embeddings_Staging(promoted_to_production)
    WHERE promoted_to_production = 0;

    PRINT 'Created idx_promoted filtered index';
END
GO

-- ==========================================
-- PART 2: Production Table (Read-Only)
-- ==========================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Embeddings_DiskANN')
BEGIN
    CREATE TABLE dbo.Embeddings_DiskANN (
        production_id INT PRIMARY KEY CLUSTERED, -- INTEGER PK required for DiskANN
        source_text NVARCHAR(MAX),
        source_type NVARCHAR(50),

        -- Full vector with DiskANN index
        embedding_full VECTOR(768),
        embedding_model NVARCHAR(100),

        -- Spatial for hybrid queries
        spatial_proj_x FLOAT,
        spatial_proj_y FLOAT,
        spatial_proj_z FLOAT,
        spatial_geometry GEOMETRY,

        -- Metadata
        dimension INT DEFAULT 768,
        original_staging_id BIGINT, -- Reference back to staging
        promoted_at DATETIME2 DEFAULT SYSUTCDATETIME()
    );

    PRINT 'Created Embeddings_DiskANN table (production, read-only after indexing)';
END
GO

-- ==========================================
-- PART 3: Promotion Process
-- ==========================================

CREATE OR ALTER PROCEDURE sp_PromoteToProduction
    @min_staging_count INT = 1000 -- Only promote when we have enough vectors
AS
BEGIN
    SET NOCOUNT ON;
    SET QUOTED_IDENTIFIER ON;
    SET ANSI_NULLS ON;

    PRINT 'PROMOTION TO PRODUCTION';
    PRINT '';

    -- Check staging count
    DECLARE @staging_count INT;
    SELECT @staging_count = COUNT(*)
    FROM dbo.Embeddings_Staging
    WHERE promoted_to_production = 0;

    PRINT '  Staging embeddings pending promotion: ' + CAST(@staging_count AS VARCHAR(10));

    IF @staging_count < @min_staging_count
    BEGIN
        PRINT '  Not enough embeddings to promote (minimum: ' + CAST(@min_staging_count AS VARCHAR(10)) + ')';
        PRINT '  Skipping promotion';
        RETURN;
    END;

    -- Check if production table has DiskANN index (if so, we can't write to it)
    IF EXISTS (
        SELECT 1
        FROM sys.indexes i
        WHERE i.object_id = OBJECT_ID('dbo.Embeddings_DiskANN')
          AND i.name LIKE '%diskann%'
          AND i.type_desc = 'VECTOR'
    )
    BEGIN
        PRINT '  ⚠ Production table has DiskANN index (read-only)';
        PRINT '  Must drop index, promote data, then rebuild index';
        PRINT '  This is a manual operation for production safety';
        RETURN;
    END;

    -- Get next production_id
    DECLARE @next_id INT = 1;
    SELECT @next_id = ISNULL(MAX(production_id), 0) + 1
    FROM dbo.Embeddings_DiskANN;

    -- Promote staging → production
    BEGIN TRANSACTION;

    INSERT INTO dbo.Embeddings_DiskANN (
        production_id, source_text, source_type,
        embedding_full, embedding_model,
        spatial_proj_x, spatial_proj_y, spatial_proj_z, spatial_geometry,
        dimension, original_staging_id, promoted_at
    )
    SELECT
        ROW_NUMBER() OVER (ORDER BY staging_id) + @next_id - 1 as production_id,
        source_text, source_type,
        embedding_full, embedding_model,
        spatial_proj_x, spatial_proj_y, spatial_proj_z, spatial_geometry,
        dimension, staging_id, SYSUTCDATETIME()
    FROM dbo.Embeddings_Staging
    WHERE promoted_to_production = 0;

    DECLARE @promoted_count INT = @@ROWCOUNT;

    -- Mark staging records as promoted
    UPDATE dbo.Embeddings_Staging
    SET promoted_to_production = 1,
        promotion_date = SYSUTCDATETIME()
    WHERE promoted_to_production = 0;

    COMMIT TRANSACTION;

    PRINT '  ✓ Promoted ' + CAST(@promoted_count AS VARCHAR(10)) + ' embeddings to production';

    -- Report current production size
    DECLARE @total_production INT;
    SELECT @total_production = COUNT(*) FROM dbo.Embeddings_DiskANN;

    PRINT '  Total production embeddings: ' + CAST(@total_production AS VARCHAR(10));

    IF @total_production >= 50000
    BEGIN
        PRINT '';
        PRINT '  ℹ Production size >= 50K vectors';
        PRINT '  Ready for DiskANN index creation';
        PRINT '  Run: EXEC sp_CreateDiskANNIndex';
    END;
END;
GO

-- ==========================================
-- PART 4: DiskANN Index Management
-- ==========================================

CREATE OR ALTER PROCEDURE sp_CreateDiskANNIndex
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'CREATING DISKANN INDEX';
    PRINT '';

    -- Verify production table has enough vectors
    DECLARE @vector_count INT;
    SELECT @vector_count = COUNT(*) FROM dbo.Embeddings_DiskANN;

    PRINT '  Production vector count: ' + CAST(@vector_count AS VARCHAR(10));

    IF @vector_count < 50000
    BEGIN
        PRINT '  ⚠ Insufficient vectors for DiskANN (minimum recommended: 50,000)';
        PRINT '  DiskANN is optimized for large-scale datasets';
        PRINT '  For < 50K vectors, use VECTOR_DISTANCE for exact search';
        RETURN;
    END;

    -- Check if index already exists
    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.Embeddings_DiskANN')
          AND name = 'idx_diskann_vector'
    )
    BEGIN
        PRINT '  Index already exists: idx_diskann_vector';
        PRINT '  Dropping existing index...';

        DROP INDEX idx_diskann_vector ON dbo.Embeddings_DiskANN;
        PRINT '  ✓ Existing index dropped';
    END;

    -- Create DiskANN vector index
    -- COMMENTED OUT: RC1 limitation - index makes table read-only
    -- Will be enabled when GA is released
    /*
    PRINT '  Creating DiskANN index (this may take several minutes)...';

    CREATE VECTOR INDEX idx_diskann_vector
    ON dbo.Embeddings_DiskANN(embedding_full)
    WITH (
        METRIC = 'cosine',
        TYPE = 'DiskANN',
        MAXDOP = 0
    );

    PRINT '  ✓ DiskANN index created';
    */

    PRINT '  ℹ DiskANN index creation commented out (RC1 limitation)';
    PRINT '  Queries will use VECTOR_DISTANCE (slower but functional)';
    PRINT '  Index will be enabled in GA release';
END;
GO

-- ==========================================
-- PART 5: Production Rebuild Process
-- ==========================================

CREATE OR ALTER PROCEDURE sp_RebuildProduction
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'REBUILDING PRODUCTION TABLE';
    PRINT 'This drops DiskANN index, promotes staging data, rebuilds index';
    PRINT '';

    -- Step 1: Drop DiskANN index (makes table writable again)
    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.Embeddings_DiskANN')
          AND name = 'idx_diskann_vector'
    )
    BEGIN
        PRINT '[1/3] Dropping DiskANN index...';
        DROP INDEX idx_diskann_vector ON dbo.Embeddings_DiskANN;
        PRINT '  ✓ Index dropped (table now writable)';
    END
    ELSE
    BEGIN
        PRINT '[1/3] No DiskANN index found (skipping)';
    END;

    -- Step 2: Promote staging data
    PRINT '[2/3] Promoting staging data...';
    EXEC sp_PromoteToProduction @min_staging_count = 1; -- Promote any amount

    -- Step 3: Recreate DiskANN index
    PRINT '[3/3] Recreating DiskANN index...';
    EXEC sp_CreateDiskANNIndex;

    PRINT '';
    PRINT '✓ Production rebuild complete';
END;
GO

-- ==========================================
-- PART 6: Query Routing
-- ==========================================

CREATE OR ALTER PROCEDURE sp_SmartVectorSearch
    @query_vector VECTOR(768),
    @top_k INT = 10,
    @search_strategy NVARCHAR(20) = 'auto' -- 'auto', 'exact', 'diskann', 'hybrid'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @production_count INT, @staging_count INT;
    SELECT @production_count = COUNT(*) FROM dbo.Embeddings_DiskANN;
    SELECT @staging_count = COUNT(*) FROM dbo.Embeddings_Staging WHERE promoted_to_production = 0;

    DECLARE @has_diskann BIT = 0;
    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.Embeddings_DiskANN')
          AND name = 'idx_diskann_vector'
    )
        SET @has_diskann = 1;

    -- Auto-select strategy
    IF @search_strategy = 'auto'
    BEGIN
        IF @production_count + @staging_count < 50000
            SET @search_strategy = 'exact';
        ELSE IF @has_diskann = 1
            SET @search_strategy = 'hybrid'; -- DiskANN on production + exact on staging
        ELSE
            SET @search_strategy = 'exact';
    END;

    PRINT 'Strategy: ' + @search_strategy;
    PRINT 'Production: ' + CAST(@production_count AS VARCHAR) + ' | Staging: ' + CAST(@staging_count AS VARCHAR);

    -- Execute based on strategy
    IF @search_strategy = 'diskann' AND @has_diskann = 1
    BEGIN
        -- DiskANN search on production only
        SELECT TOP (@top_k)
            production_id as embedding_id,
            source_text,
            VECTOR_DISTANCE('cosine', embedding_full, @query_vector) as distance
        FROM dbo.Embeddings_DiskANN
        ORDER BY VECTOR_DISTANCE('cosine', embedding_full, @query_vector);
    END
    ELSE IF @search_strategy = 'hybrid' AND @has_diskann = 1
    BEGIN
        -- DiskANN on production + exact on staging, merged
        SELECT TOP (@top_k) * FROM (
            SELECT
                production_id as embedding_id,
                source_text,
                'production' as source_table,
                VECTOR_DISTANCE('cosine', embedding_full, @query_vector) as distance
            FROM dbo.Embeddings_DiskANN

            UNION ALL

            SELECT
                staging_id as embedding_id,
                source_text,
                'staging' as source_table,
                VECTOR_DISTANCE('cosine', embedding_full, @query_vector) as distance
            FROM dbo.Embeddings_Staging
            WHERE promoted_to_production = 0
        ) combined
        ORDER BY distance;
    END
    ELSE
    BEGIN
        -- Exact search across both tables
        SELECT TOP (@top_k) * FROM (
            SELECT
                production_id as embedding_id,
                source_text,
                'production' as source_table,
                VECTOR_DISTANCE('cosine', embedding_full, @query_vector) as distance
            FROM dbo.Embeddings_DiskANN

            UNION ALL

            SELECT
                staging_id as embedding_id,
                source_text,
                'staging' as source_table,
                VECTOR_DISTANCE('cosine', embedding_full, @query_vector) as distance
            FROM dbo.Embeddings_Staging
            WHERE promoted_to_production = 0
        ) combined
        ORDER BY distance;
    END;
END;
GO

PRINT '';
PRINT '============================================================';
PRINT 'DISKANN PATTERN CREATED';
PRINT '============================================================';
PRINT 'Tables:';
PRINT '  - Embeddings_Staging (writable)';
PRINT '  - Embeddings_DiskANN (read-only with index)';
PRINT '';
PRINT 'Procedures:';
PRINT '  1. sp_PromoteToProduction - Move staging → production';
PRINT '  2. sp_CreateDiskANNIndex - Build DiskANN index';
PRINT '  3. sp_RebuildProduction - Full rebuild cycle';
PRINT '  4. sp_SmartVectorSearch - Auto-routing queries';
PRINT '';
PRINT 'Workflow:';
PRINT '  1. Insert new embeddings to Embeddings_Staging';
PRINT '  2. When staging reaches threshold (50K+), run:';
PRINT '     EXEC sp_RebuildProduction';
PRINT '  3. Query with: EXEC sp_SmartVectorSearch @query_vector';
PRINT '============================================================';
GO

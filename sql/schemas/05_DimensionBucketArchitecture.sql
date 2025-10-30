-- =====================================================================================
-- Dimension Bucket Architecture for Variable-Dimension Model Weights
-- =====================================================================================
-- Purpose: Store model weights with different embedding dimensions in optimal,
--          dimension-specific tables while maintaining unified query interface
--
-- Architecture:
--   - Physical: Dimension-specific tables (Weights_768, Weights_1536, etc.)
--   - Logical: ModelArchitecture catalog + WeightCatalog cross-reference
--   - Interface: Routing stored procedures for unified access
--
-- Benefits:
--   - No storage waste (vs padding to max dimension)
--   - Native VECTOR indexing per dimension
--   - DiskANN works optimally per table
--   - Index-only scans possible
--   - Mathematically correct (can't compare different dimensions anyway)
-- =====================================================================================

USE Hartonomous;
GO

-- =====================================================================================
-- PART 1: Model Architecture Catalog
-- =====================================================================================

IF OBJECT_ID('dbo.ModelArchitecture', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ModelArchitecture (
        model_id INT IDENTITY(1,1) PRIMARY KEY,
        model_name NVARCHAR(255) NOT NULL,
        model_type NVARCHAR(100) NOT NULL, -- 'transformer', 'diffusion', etc.
        embedding_dimension INT NOT NULL,  -- 768, 1536, 1998, 3996
        weights_table_name NVARCHAR(100) NOT NULL, -- 'Weights_768', etc.
        layer_count INT NOT NULL,
        parameter_count BIGINT NULL,
        architecture_config NVARCHAR(MAX) NULL, -- JSON metadata
        created_date DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        last_modified_date DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        is_active BIT NOT NULL DEFAULT 1,

        CONSTRAINT UQ_ModelName UNIQUE (model_name),
        CONSTRAINT CK_EmbeddingDimension CHECK (
            embedding_dimension IN (768, 1536, 1998, 3996)
        ),
        CONSTRAINT CK_WeightsTable CHECK (
            weights_table_name IN ('Weights_768', 'Weights_1536', 'Weights_1998', 'Weights_3996')
        )
    );

    CREATE INDEX IX_ModelArchitecture_Dimension
        ON dbo.ModelArchitecture(embedding_dimension);

    CREATE INDEX IX_ModelArchitecture_Table
        ON dbo.ModelArchitecture(weights_table_name)
        INCLUDE (model_id, embedding_dimension);
END
GO

-- =====================================================================================
-- PART 2: Physical Weight Storage Tables (Dimension-Specific)
-- =====================================================================================

-- ------------------------
-- Common: VECTOR(768)
-- BERT, GPT-2, most transformer models
-- ------------------------
IF OBJECT_ID('dbo.Weights_768', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Weights_768 (
        weight_id BIGINT IDENTITY(1,1) PRIMARY KEY,
        model_id INT NOT NULL,
        layer_idx INT NOT NULL,
        component_type NVARCHAR(50) NOT NULL, -- 'attention_query', 'attention_key', 'feedforward', etc.
        head_idx INT NULL, -- For multi-head attention
        from_position INT NULL,
        to_position INT NULL,
        weight_vector VECTOR(768) NOT NULL,

        -- Covering index columns for index-only scans
        importance_score FLOAT NULL,
        last_updated DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

        CONSTRAINT FK_Weights_768_Model
            FOREIGN KEY (model_id)
            REFERENCES dbo.ModelArchitecture(model_id)
    );

    -- Covering index for inference queries (index-only scan)
    CREATE NONCLUSTERED INDEX IX_Weights_768_ModelLayer
        ON dbo.Weights_768(model_id, layer_idx)
        INCLUDE (component_type, head_idx, weight_vector, importance_score);

    -- Index for importance-based queries (student model extraction)
    CREATE NONCLUSTERED INDEX IX_Weights_768_Importance
        ON dbo.Weights_768(model_id, importance_score DESC)
        INCLUDE (layer_idx, weight_vector)
        WHERE importance_score IS NOT NULL;
END
GO

-- NOTE: VECTOR index creation requires table to be read-only in SQL Server 2025 RC
-- After initial ingestion, execute:
-- ALTER TABLE dbo.Weights_768 SET (READ_ONLY = ON);
-- CREATE VECTOR INDEX idx_diskann_768 ON dbo.Weights_768(weight_vector) USING DISKANN;

-- ------------------------
-- Standard: VECTOR(1536)
-- OpenAI embeddings, larger models
-- ------------------------
IF OBJECT_ID('dbo.Weights_1536', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Weights_1536 (
        weight_id BIGINT IDENTITY(1,1) PRIMARY KEY,
        model_id INT NOT NULL,
        layer_idx INT NOT NULL,
        component_type NVARCHAR(50) NOT NULL,
        head_idx INT NULL,
        from_position INT NULL,
        to_position INT NULL,
        weight_vector VECTOR(1536) NOT NULL,

        importance_score FLOAT NULL,
        last_updated DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

        CONSTRAINT FK_Weights_1536_Model
            FOREIGN KEY (model_id)
            REFERENCES dbo.ModelArchitecture(model_id)
    );

    CREATE NONCLUSTERED INDEX IX_Weights_1536_ModelLayer
        ON dbo.Weights_1536(model_id, layer_idx)
        INCLUDE (component_type, head_idx, weight_vector, importance_score);

    CREATE NONCLUSTERED INDEX IX_Weights_1536_Importance
        ON dbo.Weights_1536(model_id, importance_score DESC)
        INCLUDE (layer_idx, weight_vector)
        WHERE importance_score IS NOT NULL;
END
GO

-- ------------------------
-- Large: VECTOR(1998)
-- Max float32 dimension
-- ------------------------
IF OBJECT_ID('dbo.Weights_1998', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Weights_1998 (
        weight_id BIGINT IDENTITY(1,1) PRIMARY KEY,
        model_id INT NOT NULL,
        layer_idx INT NOT NULL,
        component_type NVARCHAR(50) NOT NULL,
        head_idx INT NULL,
        from_position INT NULL,
        to_position INT NULL,
        weight_vector VECTOR(1998) NOT NULL,

        importance_score FLOAT NULL,
        last_updated DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

        CONSTRAINT FK_Weights_1998_Model
            FOREIGN KEY (model_id)
            REFERENCES dbo.ModelArchitecture(model_id)
    );

    CREATE NONCLUSTERED INDEX IX_Weights_1998_ModelLayer
        ON dbo.Weights_1998(model_id, layer_idx)
        INCLUDE (component_type, head_idx, weight_vector, importance_score);

    CREATE NONCLUSTERED INDEX IX_Weights_1998_Importance
        ON dbo.Weights_1998(model_id, importance_score DESC)
        INCLUDE (layer_idx, weight_vector)
        WHERE importance_score IS NOT NULL;
END
GO

-- ------------------------
-- Extra Large: VECTOR(3996, float16)
-- Max float16 dimension
-- Requires: PREVIEW_FEATURES ON
-- ------------------------
-- NOTE: Uncomment when float16 is needed
/*
IF OBJECT_ID('dbo.Weights_3996', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Weights_3996 (
        weight_id BIGINT IDENTITY(1,1) PRIMARY KEY,
        model_id INT NOT NULL,
        layer_idx INT NOT NULL,
        component_type NVARCHAR(50) NOT NULL,
        head_idx INT NULL,
        from_position INT NULL,
        to_position INT NULL,
        weight_vector VECTOR(3996, float16) NOT NULL,

        importance_score FLOAT NULL,
        last_updated DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

        CONSTRAINT FK_Weights_3996_Model
            FOREIGN KEY (model_id)
            REFERENCES dbo.ModelArchitecture(model_id)
    );

    CREATE NONCLUSTERED INDEX IX_Weights_3996_ModelLayer
        ON dbo.Weights_3996(model_id, layer_idx)
        INCLUDE (component_type, head_idx, weight_vector, importance_score);

    CREATE NONCLUSTERED INDEX IX_Weights_3996_Importance
        ON dbo.Weights_3996(model_id, importance_score DESC)
        INCLUDE (layer_idx, weight_vector)
        WHERE importance_score IS NOT NULL;
END
GO
*/

-- =====================================================================================
-- PART 3: Weight Catalog (Cross-Reference & Metadata)
-- =====================================================================================

IF OBJECT_ID('dbo.WeightCatalog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WeightCatalog (
        catalog_id BIGINT IDENTITY(1,1) PRIMARY KEY,
        weight_id BIGINT NOT NULL, -- References weight in dimension-specific table
        model_id INT NOT NULL,
        layer_idx INT NOT NULL,
        component_type NVARCHAR(50) NOT NULL,
        position_metadata NVARCHAR(MAX) NULL, -- JSON: {head_idx, from_pos, to_pos, etc.}
        importance_score FLOAT NULL,
        content_hash BINARY(32) NOT NULL, -- SHA256 for deduplication
        created_date DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

        CONSTRAINT FK_WeightCatalog_Model
            FOREIGN KEY (model_id)
            REFERENCES dbo.ModelArchitecture(model_id)
    );

    -- Index for model-based queries
    CREATE NONCLUSTERED INDEX IX_WeightCatalog_Model
        ON dbo.WeightCatalog(model_id, layer_idx)
        INCLUDE (weight_id, component_type, importance_score);

    -- Index for deduplication queries (within same dimension)
    CREATE NONCLUSTERED INDEX IX_WeightCatalog_Hash
        ON dbo.WeightCatalog(content_hash, model_id);

    -- Index for importance-based queries
    CREATE NONCLUSTERED INDEX IX_WeightCatalog_Importance
        ON dbo.WeightCatalog(importance_score DESC)
        WHERE importance_score IS NOT NULL;
END
GO

-- =====================================================================================
-- PART 4: Routing Layer Stored Procedures
-- =====================================================================================

-- Procedure: Get model dimension and routing table
CREATE OR ALTER PROCEDURE dbo.sp_GetModelRouting
    @model_id INT,
    @dimension INT OUTPUT,
    @table_name NVARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        @dimension = embedding_dimension,
        @table_name = weights_table_name
    FROM dbo.ModelArchitecture
    WHERE model_id = @model_id AND is_active = 1;

    IF @table_name IS NULL
        THROW 50001, 'Model not found or inactive', 1;
END
GO

-- Procedure: Extract student model (within same dimension)
CREATE OR ALTER PROCEDURE dbo.sp_ExtractStudentModel
    @parent_model_id INT,
    @layer_indices NVARCHAR(MAX), -- Comma-separated: '0,1,2'
    @min_importance FLOAT = 0.0,
    @max_weights INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @dimension INT, @table_name NVARCHAR(100);

    -- Get routing info
    EXEC dbo.sp_GetModelRouting
        @model_id = @parent_model_id,
        @dimension = @dimension OUTPUT,
        @table_name = @table_name OUTPUT;

    -- Build dynamic SQL for correct table
    DECLARE @sql NVARCHAR(MAX);

    SET @sql = '
        SELECT ' + CASE WHEN @max_weights IS NOT NULL
                        THEN 'TOP (@max_weights_param) '
                        ELSE '' END + '
            weight_id,
            model_id,
            layer_idx,
            component_type,
            head_idx,
            from_position,
            to_position,
            weight_vector,
            importance_score
        FROM dbo.' + QUOTENAME(@table_name) + '
        WHERE model_id = @parent_model_id_param
            AND layer_idx IN (SELECT value FROM STRING_SPLIT(@layer_indices_param, '',''))
            AND (importance_score IS NULL OR importance_score >= @min_importance_param)
        ORDER BY importance_score DESC NULLS LAST';

    -- Execute with parameters
    EXEC sp_executesql @sql,
        N'@parent_model_id_param INT, @layer_indices_param NVARCHAR(MAX), @min_importance_param FLOAT, @max_weights_param INT',
        @parent_model_id_param = @parent_model_id,
        @layer_indices_param = @layer_indices,
        @min_importance_param = @min_importance,
        @max_weights_param = @max_weights;
END
GO

-- Procedure: Find duplicate weights (within same dimension)
CREATE OR ALTER PROCEDURE dbo.sp_FindDuplicateWeights
    @model_id INT = NULL,
    @min_duplicates INT = 2
AS
BEGIN
    SET NOCOUNT ON;

    -- Get dimension if model specified
    DECLARE @dimension INT;

    IF @model_id IS NOT NULL
    BEGIN
        SELECT @dimension = embedding_dimension
        FROM dbo.ModelArchitecture
        WHERE model_id = @model_id;
    END

    -- Find duplicates within dimension class
    SELECT
        wc.content_hash,
        ma.embedding_dimension,
        COUNT(*) as duplicate_count,
        STRING_AGG(CAST(wc.model_id AS NVARCHAR), ',') as model_ids
    FROM dbo.WeightCatalog wc
    JOIN dbo.ModelArchitecture ma ON wc.model_id = ma.model_id
    WHERE (@dimension IS NULL OR ma.embedding_dimension = @dimension)
    GROUP BY wc.content_hash, ma.embedding_dimension
    HAVING COUNT(*) >= @min_duplicates
    ORDER BY COUNT(*) DESC;
END
GO

PRINT 'Dimension bucket architecture created successfully!'
PRINT 'Next steps:'
PRINT '  1. Ingest models via routing layer'
PRINT '  2. After ingestion, set tables to READ_ONLY and create VECTOR indexes'
PRINT '  3. Use sp_ExtractStudentModel for knowledge distillation'
GO

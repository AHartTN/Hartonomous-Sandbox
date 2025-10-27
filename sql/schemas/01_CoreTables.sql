-- =============================================
-- Hartonomous Core Tables
-- SQL Server 2025 RC1+
-- =============================================
-- 
-- ⚠️ DEPRECATED: This file is for REFERENCE ONLY
-- 
-- The database schema is now managed by EF Core Code First migrations.
-- See: src/Hartonomous.Data/Migrations/
-- 
-- This SQL file documents the intended schema design but is NOT deployed.
-- Use `dotnet ef migrations add` and `dotnet ef database update` instead.
-- 
-- Only SQL CLR assemblies (src/SqlClr/) and stored procedures that EF
-- cannot express (sql/procedures/) are deployed via scripts/deploy.ps1.
-- =============================================

USE master;
GO

-- Create database if not exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Hartonomous')
BEGIN
    CREATE DATABASE Hartonomous;
    PRINT 'Database Hartonomous created successfully.';
END
GO

USE Hartonomous;
GO

-- Enable required features
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
GO

EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
GO

PRINT 'Core configuration completed.';
GO

-- =============================================
-- Model Management Tables
-- =============================================

-- Models: Store metadata about ingested AI models
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Models]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.Models (
        model_id INT PRIMARY KEY IDENTITY(1,1),
        model_name NVARCHAR(200) NOT NULL,
        model_type NVARCHAR(100) NOT NULL,
        architecture NVARCHAR(100),
        config JSON, -- FIXED: Native JSON type (SQL Server 2025)
        parameter_count BIGINT,
        ingestion_date DATETIME2 DEFAULT SYSUTCDATETIME(),
        last_used DATETIME2,
        usage_count BIGINT DEFAULT 0,
        average_inference_ms FLOAT
    );
    PRINT 'Table dbo.Models created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_model_name' AND object_id = OBJECT_ID('dbo.Models'))
BEGIN
    CREATE INDEX idx_model_name ON dbo.Models(model_name);
    PRINT 'Index idx_model_name on dbo.Models created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_model_type' AND object_id = OBJECT_ID('dbo.Models'))
BEGIN
    CREATE INDEX idx_model_type ON dbo.Models(model_type);
    PRINT 'Index idx_model_type on dbo.Models created.';
END
GO

-- ModelLayers: Store model architecture layer-by-layer
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ModelLayers]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.ModelLayers (
        layer_id BIGINT PRIMARY KEY IDENTITY(1,1),
        model_id INT NOT NULL,
        layer_idx INT NOT NULL,
        layer_name NVARCHAR(100),
        layer_type NVARCHAR(50),
        weights VARBINARY(MAX), -- JUSTIFIED: Large binary tensor weights, no better type
        weights_compressed VARBINARY(MAX), -- JUSTIFIED: Compressed binary data
        quantization_type NVARCHAR(20),
        quantization_scale FLOAT,
        quantization_zero_point FLOAT,
        parameters JSON, -- FIXED: Native JSON type for layer hyperparameters
        parameter_count BIGINT,
        cache_hit_rate FLOAT DEFAULT 0.0,
        avg_compute_time_ms FLOAT,
        FOREIGN KEY (model_id) REFERENCES dbo.Models(model_id) ON DELETE CASCADE
    );
    PRINT 'Table dbo.ModelLayers created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_model_layer' AND object_id = OBJECT_ID('dbo.ModelLayers'))
BEGIN
    CREATE INDEX idx_model_layer ON dbo.ModelLayers(model_id, layer_idx);
    PRINT 'Index idx_model_layer on dbo.ModelLayers created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_layer_type' AND object_id = OBJECT_ID('dbo.ModelLayers'))
BEGIN
    CREATE INDEX idx_layer_type ON dbo.ModelLayers(layer_type);
    PRINT 'Index idx_layer_type on dbo.ModelLayers created.';
END
GO

-- CachedActivations: Pre-computed layer outputs for common inputs
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CachedActivations]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.CachedActivations (
        cache_id BIGINT PRIMARY KEY IDENTITY(1,1),
        model_id INT NOT NULL,
        layer_id BIGINT NOT NULL,
        input_hash BINARY(32) NOT NULL,
        activation_output VARBINARY(MAX), -- JUSTIFIED: Tensor activations, no better type
        output_shape NVARCHAR(100),
        hit_count BIGINT DEFAULT 0,
        created_date DATETIME2 DEFAULT SYSUTCDATETIME(),
        last_accessed DATETIME2 DEFAULT SYSUTCDATETIME(),
        compute_time_saved_ms BIGINT DEFAULT 0,
        FOREIGN KEY (model_id) REFERENCES dbo.Models(model_id) ON DELETE CASCADE,
        FOREIGN KEY (layer_id) REFERENCES dbo.ModelLayers(layer_id)
    );
    PRINT 'Table dbo.CachedActivations created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_cache_lookup' AND object_id = OBJECT_ID('dbo.CachedActivations'))
BEGIN
    CREATE UNIQUE INDEX idx_cache_lookup ON dbo.CachedActivations(model_id, layer_id, input_hash);
    PRINT 'Index idx_cache_lookup on dbo.CachedActivations created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_cache_usage' AND object_id = OBJECT_ID('dbo.CachedActivations'))
BEGIN
    CREATE INDEX idx_cache_usage ON dbo.CachedActivations(last_accessed DESC, hit_count DESC);
    PRINT 'Index idx_cache_usage on dbo.CachedActivations created.';
END
GO

-- ModelMetadata: Additional model information and capabilities
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ModelMetadata]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.ModelMetadata (
        metadata_id INT PRIMARY KEY IDENTITY(1,1),
        model_id INT NOT NULL UNIQUE,
        supported_tasks JSON, -- FIXED: Native JSON array
        supported_modalities JSON, -- FIXED: Native JSON array
        max_input_length INT,
        max_output_length INT,
        embedding_dimension INT,
        performance_metrics JSON, -- FIXED: Native JSON object
        training_dataset NVARCHAR(500),
        training_date DATE,
        license NVARCHAR(100),
        source_url NVARCHAR(500),
        FOREIGN KEY (model_id) REFERENCES dbo.Models(model_id) ON DELETE CASCADE
    );
    PRINT 'Table dbo.ModelMetadata created.';
END
GO

-- =============================================
-- Inference Tracking Tables
-- =============================================

-- InferenceRequests: Log all inference operations
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InferenceRequests]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.InferenceRequests (
        inference_id BIGINT PRIMARY KEY IDENTITY(1,1),
        request_timestamp DATETIME2 DEFAULT SYSUTCDATETIME(),
        task_type NVARCHAR(50),
        input_data JSON, -- FIXED: Native JSON for structured input
        input_hash BINARY(32),
        models_used NVARCHAR(500),
        ensemble_strategy NVARCHAR(50),
        output_data JSON, -- FIXED: Native JSON for structured output
        output_metadata JSON, -- FIXED: Native JSON for timing, confidence scores
        total_duration_ms INT,
        cache_hit BIT DEFAULT 0,
        user_rating TINYINT,
        user_feedback NVARCHAR(MAX) -- Free-form text, justified
    );
    PRINT 'Table dbo.InferenceRequests created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_timestamp' AND object_id = OBJECT_ID('dbo.InferenceRequests'))
BEGIN
    CREATE INDEX idx_timestamp ON dbo.InferenceRequests(request_timestamp DESC);
    PRINT 'Index idx_timestamp on dbo.InferenceRequests created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_task_type' AND object_id = OBJECT_ID('dbo.InferenceRequests'))
BEGIN
    CREATE INDEX idx_task_type ON dbo.InferenceRequests(task_type);
    PRINT 'Index idx_task_type on dbo.InferenceRequests created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_input_hash' AND object_id = OBJECT_ID('dbo.InferenceRequests'))
BEGIN
    CREATE INDEX idx_input_hash ON dbo.InferenceRequests(input_hash);
    PRINT 'Index idx_input_hash on dbo.InferenceRequests created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_cache_hit' AND object_id = OBJECT_ID('dbo.InferenceRequests'))
BEGIN
    CREATE INDEX idx_cache_hit ON dbo.InferenceRequests(cache_hit);
    PRINT 'Index idx_cache_hit on dbo.InferenceRequests created.';
END
GO

-- InferenceSteps: Detailed breakdown of multi-step inference
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InferenceSteps]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.InferenceSteps (
        step_id BIGINT PRIMARY KEY IDENTITY(1,1),
        inference_id BIGINT NOT NULL,
        step_number INT NOT NULL,
        model_id INT,
        layer_id BIGINT,
        operation_type NVARCHAR(50),
        query_text NVARCHAR(MAX), -- JUSTIFIED: SQL query text can be long, no JSON structure
        index_used NVARCHAR(200),
        rows_examined BIGINT,
        rows_returned BIGINT,
        duration_ms INT,
        cache_used BIT DEFAULT 0,
        FOREIGN KEY (inference_id) REFERENCES dbo.InferenceRequests(inference_id) ON DELETE CASCADE,
        FOREIGN KEY (model_id) REFERENCES dbo.Models(model_id)
    );
    PRINT 'Table dbo.InferenceSteps created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_inference_steps' AND object_id = OBJECT_ID('dbo.InferenceSteps'))
BEGIN
    CREATE INDEX idx_inference_steps ON dbo.InferenceSteps(inference_id, step_number);
    PRINT 'Index idx_inference_steps on dbo.InferenceSteps created.';
END
GO

-- =============================================
-- Vocabulary and Embeddings
-- =============================================

-- TokenVocabulary: Store tokenizer vocabularies and embeddings
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TokenVocabulary]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.TokenVocabulary (
        vocab_id BIGINT PRIMARY KEY IDENTITY(1,1),
        model_id INT NOT NULL,
        token NVARCHAR(100) NOT NULL,
        token_id INT NOT NULL,
        token_type NVARCHAR(20),
        embedding VECTOR(768), -- FIXED: Use native VECTOR type instead of VARBINARY
        embedding_dim INT,
        frequency BIGINT DEFAULT 0,
        last_used DATETIME2,
        FOREIGN KEY (model_id) REFERENCES dbo.Models(model_id) ON DELETE CASCADE
    );
    PRINT 'Table dbo.TokenVocabulary created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_model_token' AND object_id = OBJECT_ID('dbo.TokenVocabulary'))
BEGIN
    CREATE UNIQUE INDEX idx_model_token ON dbo.TokenVocabulary(model_id, token_id);
    PRINT 'Index idx_model_token on dbo.TokenVocabulary created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_token_text' AND object_id = OBJECT_ID('dbo.TokenVocabulary'))
BEGIN
    CREATE INDEX idx_token_text ON dbo.TokenVocabulary(model_id, token);
    PRINT 'Index idx_token_text on dbo.TokenVocabulary created.';
END
GO

PRINT 'Core tables script completed.';
GO
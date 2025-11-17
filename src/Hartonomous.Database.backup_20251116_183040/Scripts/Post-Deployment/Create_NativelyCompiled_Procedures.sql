/*
================================================================================
Natively-Compiled Stored Procedures - Post-Deployment
================================================================================
Purpose: Create NATIVE_COMPILATION procedures for In-Memory OLTP tables

WHY POST-DEPLOYMENT:
- Natively-compiled procedures with SCHEMABINDING require tables to exist BEFORE compilation
- Must be created AFTER In-Memory tables in Setup_InMemory_Tables.sql
- Compiled to native machine code for maximum performance

PERFORMANCE BENEFITS:
- 10-100x faster execution vs interpreted T-SQL
- Zero lock/latch overhead (optimistic concurrency)
- Direct memory access (no buffer pool)
- CPU instruction-level optimization

RESTRICTIONS:
- SCHEMABINDING required (table schema locked)
- Limited T-SQL surface area (no CTEs, no MERGE, no dynamic SQL)
- Must use BEGIN ATOMIC blocks
- All referenced objects must be in same database

================================================================================
*/

PRINT '';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT 'HEKATON: Creating Natively-Compiled Procedures';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT '';

-- =============================================
-- PROCEDURE 1: sp_InsertBillingUsageRecord_Native
-- Billing insert operation (already exists, recreating for safety)
-- =============================================
IF OBJECT_ID('dbo.sp_InsertBillingUsageRecord_Native', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_InsertBillingUsageRecord_Native;
GO

PRINT '[1/6] Creating sp_InsertBillingUsageRecord_Native...';
GO

CREATE PROCEDURE dbo.sp_InsertBillingUsageRecord_Native
    @TenantId NVARCHAR(128),
    @PrincipalId NVARCHAR(256),
    @Operation NVARCHAR(128),
    @MessageType NVARCHAR(128) = NULL,
    @Handler NVARCHAR(256) = NULL,
    @Units DECIMAL(18,6),
    @BaseRate DECIMAL(18,6),
    @Multiplier DECIMAL(18,6) = 1.0,
    @TotalCost DECIMAL(18,6),
    @MetadataJson NVARCHAR(MAX) = NULL
WITH NATIVE_COMPILATION, SCHEMABINDING, EXECUTE AS OWNER
AS
BEGIN ATOMIC WITH (TRANSACTION ISOLATION LEVEL = SNAPSHOT, LANGUAGE = N'us_english')
    INSERT INTO dbo.BillingUsageLedger_InMemory
    (TenantId, PrincipalId, Operation, MessageType, Handler, Units, BaseRate, Multiplier, TotalCost, MetadataJson, TimestampUtc)
    VALUES
    (@TenantId, @PrincipalId, @Operation, @MessageType, @Handler, @Units, @BaseRate, @Multiplier, @TotalCost, @MetadataJson, SYSUTCDATETIME());
END;
GO

PRINT '  ✓ sp_InsertBillingUsageRecord_Native created';
GO

-- =============================================
-- PROCEDURE 2: sp_GetInferenceCacheHit_Native
-- Inference cache lookup operation
-- =============================================
IF OBJECT_ID('dbo.sp_GetInferenceCacheHit_Native', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetInferenceCacheHit_Native;
GO

PRINT '[2/6] Creating sp_GetInferenceCacheHit_Native...';
GO

CREATE PROCEDURE dbo.sp_GetInferenceCacheHit_Native
    @CacheKey NVARCHAR(64),
    @OutputData VARBINARY(MAX) OUTPUT,
    @ComputeTimeMs FLOAT OUTPUT,
    @Found BIT OUTPUT
WITH NATIVE_COMPILATION, SCHEMABINDING, EXECUTE AS OWNER
AS
BEGIN ATOMIC WITH (TRANSACTION ISOLATION LEVEL = SNAPSHOT, LANGUAGE = N'us_english')
    
    SELECT TOP 1
        @OutputData = OutputData,
        @ComputeTimeMs = ComputeTimeMs,
        @Found = 1
    FROM dbo.InferenceCache_InMemory  -- Removed WITH (SNAPSHOT) hint (not supported in natively-compiled)
    WHERE CacheKey = @CacheKey;
    
    -- Update access tracking (optimistic concurrency, no locks)
    IF @Found = 1
    BEGIN
        UPDATE dbo.InferenceCache_InMemory
        SET LastAccessedUtc = SYSUTCDATETIME(),
            AccessCount = AccessCount + 1
        WHERE CacheKey = @CacheKey;
    END
    ELSE
    BEGIN
        SET @OutputData = NULL;
        SET @ComputeTimeMs = NULL;
        SET @Found = 0;
    END
END;
GO

PRINT '  ✓ sp_GetInferenceCacheHit_Native created';
GO

-- =============================================
-- PROCEDURE 3: sp_InsertInferenceCache_Native
-- Inference cache write operation
-- =============================================
IF OBJECT_ID('dbo.sp_InsertInferenceCache_Native', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_InsertInferenceCache_Native;
GO

PRINT '[3/6] Creating sp_InsertInferenceCache_Native...';
GO

CREATE PROCEDURE dbo.sp_InsertInferenceCache_Native
    @CacheKey NVARCHAR(64),
    @ModelId INT,
    @InferenceType NVARCHAR(100),
    @InputHash BINARY(32),
    @OutputData VARBINARY(MAX),
    @IntermediateStates VARBINARY(MAX) = NULL,
    @SizeBytes BIGINT = NULL,
    @ComputeTimeMs FLOAT = NULL
WITH NATIVE_COMPILATION, SCHEMABINDING, EXECUTE AS OWNER
AS
BEGIN ATOMIC WITH (TRANSACTION ISOLATION LEVEL = SNAPSHOT, LANGUAGE = N'us_english')
    
    -- Upsert pattern: Try update first, insert if not exists
    DECLARE @RowCount INT;
    
    UPDATE dbo.InferenceCache_InMemory
    SET OutputData = @OutputData,
        IntermediateStates = @IntermediateStates,
        LastAccessedUtc = SYSUTCDATETIME(),
        AccessCount = AccessCount + 1,
        SizeBytes = @SizeBytes,
        ComputeTimeMs = @ComputeTimeMs
    WHERE CacheKey = @CacheKey;
    
    SET @RowCount = @@ROWCOUNT;
    
    IF @RowCount = 0
    BEGIN
        INSERT INTO dbo.InferenceCache_InMemory
        (CacheKey, ModelId, InferenceType, InputHash, OutputData, IntermediateStates, SizeBytes, ComputeTimeMs, CreatedUtc, LastAccessedUtc, AccessCount)
        VALUES
        (@CacheKey, @ModelId, @InferenceType, @InputHash, @OutputData, @IntermediateStates, @SizeBytes, @ComputeTimeMs, SYSUTCDATETIME(), SYSUTCDATETIME(), 1);
    END
END;
GO

PRINT '  ✓ sp_InsertInferenceCache_Native created';
GO

-- =============================================
-- PROCEDURE 4: sp_GetCachedActivation_Native
-- Layer activation cache lookup
-- =============================================
IF OBJECT_ID('dbo.sp_GetCachedActivation_Native', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetCachedActivation_Native;
GO

PRINT '[4/6] Creating sp_GetCachedActivation_Native...';
GO

CREATE PROCEDURE dbo.sp_GetCachedActivation_Native
    @LayerId BIGINT,
    @InputHash BINARY(32),
    @ActivationOutput VARBINARY(MAX) OUTPUT,
    @OutputShape NVARCHAR(100) OUTPUT,
    @Found BIT OUTPUT
WITH NATIVE_COMPILATION, SCHEMABINDING, EXECUTE AS OWNER
AS
BEGIN ATOMIC WITH (TRANSACTION ISOLATION LEVEL = SNAPSHOT, LANGUAGE = N'us_english')
    
    SELECT TOP 1
        @ActivationOutput = ActivationOutput,
        @OutputShape = OutputShape,
        @Found = 1
    FROM dbo.CachedActivations_InMemory  -- Removed WITH (SNAPSHOT) hint (not supported in natively-compiled)
    WHERE LayerId = @LayerId AND InputHash = @InputHash;
    
    IF @Found = 1
    BEGIN
        UPDATE dbo.CachedActivations_InMemory
        SET HitCount = HitCount + 1,
            LastAccessed = SYSUTCDATETIME()
        WHERE LayerId = @LayerId AND InputHash = @InputHash;
    END
    ELSE
    BEGIN
        SET @ActivationOutput = NULL;
        SET @OutputShape = NULL;
        SET @Found = 0;
    END
END;
GO

PRINT '  ✓ sp_GetCachedActivation_Native created';
GO

-- =============================================
-- PROCEDURE 5: sp_InsertCachedActivation_Native
-- Layer activation cache write
-- =============================================
IF OBJECT_ID('dbo.sp_InsertCachedActivation_Native', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_InsertCachedActivation_Native;
GO

PRINT '[5/6] Creating sp_InsertCachedActivation_Native...';
GO

CREATE PROCEDURE dbo.sp_InsertCachedActivation_Native
    @ModelId INT,
    @LayerId BIGINT,
    @InputHash BINARY(32),
    @ActivationOutput VARBINARY(MAX),
    @OutputShape NVARCHAR(100) = NULL,
    @ComputeTimeSavedMs BIGINT = 0
WITH NATIVE_COMPILATION, SCHEMABINDING, EXECUTE AS OWNER
AS
BEGIN ATOMIC WITH (TRANSACTION ISOLATION LEVEL = SNAPSHOT, LANGUAGE = N'us_english')
    
    DECLARE @RowCount INT;
    
    UPDATE dbo.CachedActivations_InMemory
    SET ActivationOutput = @ActivationOutput,
        OutputShape = @OutputShape,
        HitCount = HitCount + 1,
        LastAccessed = SYSUTCDATETIME(),
        ComputeTimeSavedMs = ComputeTimeSavedMs + @ComputeTimeSavedMs
    WHERE LayerId = @LayerId AND InputHash = @InputHash;
    
    SET @RowCount = @@ROWCOUNT;
    
    IF @RowCount = 0
    BEGIN
        INSERT INTO dbo.CachedActivations_InMemory
        (ModelId, LayerId, InputHash, ActivationOutput, OutputShape, HitCount, CreatedDate, LastAccessed, ComputeTimeSavedMs)
        VALUES
        (@ModelId, @LayerId, @InputHash, @ActivationOutput, @OutputShape, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), @ComputeTimeSavedMs);
    END
END;
GO

PRINT '  ✓ sp_InsertCachedActivation_Native created';
GO

-- =============================================
-- PROCEDURE 6: sp_InsertSessionPath_Native
-- OODA loop session path tracking
-- =============================================
IF OBJECT_ID('dbo.sp_InsertSessionPath_Native', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_InsertSessionPath_Native;
GO

PRINT '[6/6] Creating sp_InsertSessionPath_Native...';
GO

CREATE PROCEDURE dbo.sp_InsertSessionPath_Native
    @SessionId UNIQUEIDENTIFIER,
    @PathNumber INT,
    @HypothesisId UNIQUEIDENTIFIER = NULL,
    @ResponseText NVARCHAR(MAX) = NULL,
    @ResponseVector VARBINARY(MAX) = NULL,
    @Score FLOAT = NULL,
    @IsSelected BIT = 0
WITH NATIVE_COMPILATION, SCHEMABINDING, EXECUTE AS OWNER
AS
BEGIN ATOMIC WITH (TRANSACTION ISOLATION LEVEL = SNAPSHOT, LANGUAGE = N'us_english')
    
    INSERT INTO dbo.SessionPaths_InMemory
    (SessionId, PathNumber, HypothesisId, ResponseText, ResponseVector, Score, IsSelected, CreatedUtc)
    VALUES
    (@SessionId, @PathNumber, @HypothesisId, @ResponseText, @ResponseVector, @Score, @IsSelected, SYSUTCDATETIME());
END;
GO

PRINT '  ✓ sp_InsertSessionPath_Native created';
GO

PRINT '';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT 'HEKATON: Natively-Compiled Procedures Created';
PRINT '';
PRINT 'Performance Procedures Ready:';
PRINT '  ✓ sp_InsertBillingUsageRecord_Native  (billing inserts)';
PRINT '  ✓ sp_GetInferenceCacheHit_Native      (cache lookups)';
PRINT '  ✓ sp_InsertInferenceCache_Native      (cache writes)';
PRINT '  ✓ sp_GetCachedActivation_Native       (activation cache reads)';
PRINT '  ✓ sp_InsertCachedActivation_Native    (activation cache writes)';
PRINT '  ✓ sp_InsertSessionPath_Native         (OODA loop tracking)';
PRINT '';
PRINT 'Usage Example:';
PRINT '  EXEC sp_GetInferenceCacheHit_Native @CacheKey = ''model_123_input_abc'',';
PRINT '       @OutputData = @out OUTPUT, @ComputeTimeMs = @ms OUTPUT, @Found = @hit OUTPUT;';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT '';
GO

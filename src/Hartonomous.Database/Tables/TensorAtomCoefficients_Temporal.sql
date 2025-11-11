-- ===============================================
-- Enable Temporal Tables for TensorAtomCoefficients
-- Purpose: Track weight history for autonomous learning
-- ===============================================
-- Created: 2025-11-08
-- Part of: Phase 3 - Temporal Tables Implementation
-- Reference: docs/audit/03-TEMPORAL-TABLES.md
-- ===============================================

USE Hartonomous;

-- Step 1: Add period columns if they don't exist
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TensorAtomCoefficients') AND name = 'ValidFrom')
BEGIN

    ALTER TABLE dbo.TensorAtomCoefficients
    ADD 
        ValidFrom DATETIME2(7) GENERATED ALWAYS AS ROW START NOT NULL DEFAULT SYSUTCDATETIME(),
        ValidTo DATETIME2(7) GENERATED ALWAYS AS ROW END NOT NULL DEFAULT CAST('9999-12-31 23:59:59.9999999' AS DATETIME2(7)),
        PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);

END
ELSE
BEGIN

END;

-- Step 2: Create history table if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TensorAtomCoefficients_History' AND type = 'U')
BEGIN

    CREATE TABLE dbo.TensorAtomCoefficients_History
    (
        TensorAtomCoefficientId BIGINT NOT NULL,
        TensorAtomId BIGINT NOT NULL,
        ParentLayerId BIGINT NOT NULL,
        TensorRole NVARCHAR(128) NULL,
        Coefficient REAL NOT NULL,
        ValidFrom DATETIME2(7) NOT NULL,
        ValidTo DATETIME2(7) NOT NULL,
        INDEX IX_TensorAtomCoefficients_History_Period NONCLUSTERED (ValidTo, ValidFrom)
    );

END
ELSE
BEGIN

END;

-- Step 3: Enable system versioning
IF EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'TensorAtomCoefficients' 
    AND temporal_type = 0  -- Not yet temporal
)
BEGIN

    ALTER TABLE dbo.TensorAtomCoefficients
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.TensorAtomCoefficients_History));

END
ELSE
BEGIN

END;

-- Step 4: Verify configuration
DECLARE @is_temporal BIT;
DECLARE @history_table NVARCHAR(256);

SELECT 
    @is_temporal = temporal_type,
    @history_table = OBJECT_NAME(history_table_id)
FROM sys.tables
WHERE name = 'TensorAtomCoefficients';

IF @is_temporal = 2  -- 2 = System versioned temporal table
BEGIN

    PRINT '  History Table: ' + @history_table;

END
ELSE
BEGIN
    RAISERROR('Temporal table configuration failed!', 16, 1);
END;
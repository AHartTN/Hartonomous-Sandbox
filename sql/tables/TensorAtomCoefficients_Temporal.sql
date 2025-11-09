-- ===============================================
-- Enable Temporal Tables for TensorAtomCoefficients
-- Purpose: Track weight history for autonomous learning
-- ===============================================
-- Created: 2025-11-08
-- Part of: Phase 3 - Temporal Tables Implementation
-- Reference: docs/audit/03-TEMPORAL-TABLES.md
-- ===============================================

USE Hartonomous;
GO

PRINT 'Converting TensorAtomCoefficients to temporal table...';
GO

-- Step 1: Add period columns if they don't exist
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TensorAtomCoefficients') AND name = 'ValidFrom')
BEGIN
    PRINT '  Adding ValidFrom and ValidTo columns...';
    
    ALTER TABLE dbo.TensorAtomCoefficients
    ADD 
        ValidFrom DATETIME2(7) GENERATED ALWAYS AS ROW START NOT NULL DEFAULT SYSUTCDATETIME(),
        ValidTo DATETIME2(7) GENERATED ALWAYS AS ROW END NOT NULL DEFAULT CAST('9999-12-31 23:59:59.9999999' AS DATETIME2(7)),
        PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);
    
    PRINT '  ✓ Period columns added';
END
ELSE
BEGIN
    PRINT '  ✓ Period columns already exist';
END;
GO

-- Step 2: Create history table if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TensorAtomCoefficients_History' AND type = 'U')
BEGIN
    PRINT '  Creating history table...';
    
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
    
    PRINT '  ✓ History table created';
END
ELSE
BEGIN
    PRINT '  ✓ History table already exists';
END;
GO

-- Step 3: Enable system versioning
IF EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'TensorAtomCoefficients' 
    AND temporal_type = 0  -- Not yet temporal
)
BEGIN
    PRINT '  Enabling system versioning...';
    
    ALTER TABLE dbo.TensorAtomCoefficients
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.TensorAtomCoefficients_History));
    
    PRINT '  ✓ System versioning enabled';
    PRINT '';
    PRINT '✅ TensorAtomCoefficients is now a temporal table';
    PRINT '   - All UPDATEs will preserve history automatically';
    PRINT '   - Query history with FOR SYSTEM_TIME AS OF';
    PRINT '   - Weight evolution tracking enabled';
END
ELSE
BEGIN
    PRINT '  ✓ System versioning already enabled';
END;
GO

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
    PRINT '';
    PRINT '============================================';
    PRINT 'VERIFICATION SUCCESSFUL';
    PRINT '============================================';
    PRINT '  Table: TensorAtomCoefficients';
    PRINT '  Type: Temporal (System Versioned)';
    PRINT '  History Table: ' + @history_table;
    PRINT '';
    PRINT 'Usage Examples:';
    PRINT '  -- Current weights:';
    PRINT '  SELECT * FROM TensorAtomCoefficients;';
    PRINT '';
    PRINT '  -- Weights as of 1 hour ago:';
    PRINT '  SELECT * FROM TensorAtomCoefficients';
    PRINT '    FOR SYSTEM_TIME AS OF DATEADD(HOUR, -1, SYSUTCDATETIME());';
    PRINT '';
    PRINT '  -- All weight history:';
    PRINT '  SELECT * FROM TensorAtomCoefficients';
    PRINT '    FOR SYSTEM_TIME ALL';
    PRINT '    ORDER BY ValidFrom;';
END
ELSE
BEGIN
    RAISERROR('Temporal table configuration failed!', 16, 1);
END;
GO

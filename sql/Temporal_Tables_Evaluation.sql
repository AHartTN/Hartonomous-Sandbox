-- =============================================
-- Temporal Tables Evaluation for ModelLayer and TensorAtom
-- =============================================
-- Assesses SYSTEM_VERSIONING for automatic model history tracking
-- Potential replacement for VectorDriftOverTime CLR aggregate
-- =============================================

USE Hartonomous;
GO

PRINT '=======================================================';
PRINT 'Temporal Tables Evaluation for Model History';
PRINT 'SQL Server 2016+ Feature: SYSTEM_VERSIONING';
PRINT '=======================================================';
GO

-- =============================================
-- What Are Temporal Tables?
-- =============================================

PRINT '';
PRINT '--- Temporal Tables Overview ---';
PRINT 'Temporal tables automatically track full history of data changes';
PRINT 'Benefits:';
PRINT '  • Automatic history tracking (no triggers needed)';
PRINT '  • Point-in-time queries (FOR SYSTEM_TIME AS OF)';
PRINT '  • Instant rollback to previous state';
PRINT '  • Built-in retention policies (SQL 2017+)';
PRINT '  • Zero application code changes';
PRINT '';
PRINT 'Ideal for:';
PRINT '  • Model version tracking (ModelLayer, TensorAtom)';
PRINT '  • Drift analysis over time';
PRINT '  • Audit trails for weight updates';
PRINT '  • Regulatory compliance (model provenance)';
GO

-- =============================================
-- Current State Analysis
-- =============================================

PRINT '';
PRINT '--- Current Model History Tracking ---';

-- Check if ModelLayers has history tracking
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ModelLayers')
BEGIN
    PRINT 'ModelLayers table exists';
    PRINT '  Current approach: Manual versioning or CLR aggregates?';
    
    -- Check for version-related columns
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ModelLayers') AND name LIKE '%Version%')
        PRINT '  ✓ Has version columns';
    ELSE
        PRINT '  ✗ No explicit version tracking';
END;

-- Check if TensorAtoms has history tracking
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TensorAtoms')
BEGIN
    PRINT 'TensorAtoms table exists';
    
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TensorAtoms') AND name = 'ImportanceScore')
        PRINT '  ✓ Has ImportanceScore (changes over time)';
END;
GO

-- =============================================
-- Temporal Table Design for ModelLayer
-- =============================================

PRINT '';
PRINT '--- Temporal Table Design: ModelLayer ---';
PRINT '
-- Enable temporal tracking on existing ModelLayers table

ALTER TABLE dbo.ModelLayers
ADD 
    ValidFrom DATETIME2(7) GENERATED ALWAYS AS ROW START NOT NULL DEFAULT SYSUTCDATETIME(),
    ValidTo DATETIME2(7) GENERATED ALWAYS AS ROW END NOT NULL DEFAULT CAST(''9999-12-31 23:59:59.9999999'' AS DATETIME2(7)),
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);

ALTER TABLE dbo.ModelLayers
SET (SYSTEM_VERSIONING = ON (
    HISTORY_TABLE = dbo.ModelLayersHistory,
    DATA_CONSISTENCY_CHECK = ON,
    HISTORY_RETENTION_PERIOD = 2 YEARS  -- Auto-cleanup after 2 years
));

-- Query model state at any point in time
SELECT *
FROM dbo.ModelLayers
FOR SYSTEM_TIME AS OF ''2024-01-01 00:00:00''
WHERE ModelId = 1;

-- Query all changes to a layer
SELECT ValidFrom, ValidTo, LayerType, ImportanceScore, *
FROM dbo.ModelLayers
FOR SYSTEM_TIME ALL
WHERE LayerId = 42
ORDER BY ValidFrom;
';
GO

-- =============================================
-- Temporal Table Design for TensorAtom
-- =============================================

PRINT '';
PRINT '--- Temporal Table Design: TensorAtom ---';
PRINT '
-- Enable temporal tracking on TensorAtoms

ALTER TABLE dbo.TensorAtoms
ADD 
    ValidFrom DATETIME2(7) GENERATED ALWAYS AS ROW START NOT NULL DEFAULT SYSUTCDATETIME(),
    ValidTo DATETIME2(7) GENERATED ALWAYS AS ROW END NOT NULL DEFAULT CAST(''9999-12-31 23:59:59.9999999'' AS DATETIME2(7)),
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);

ALTER TABLE dbo.TensorAtoms
SET (SYSTEM_VERSIONING = ON (
    HISTORY_TABLE = dbo.TensorAtomsHistory,
    DATA_CONSISTENCY_CHECK = ON,
    HISTORY_RETENTION_PERIOD = 1 YEAR
));

-- Track ImportanceScore drift over time
SELECT 
    TensorAtomId,
    ValidFrom,
    ValidTo,
    ImportanceScore,
    LAG(ImportanceScore) OVER (PARTITION BY TensorAtomId ORDER BY ValidFrom) AS PreviousScore,
    ImportanceScore - LAG(ImportanceScore) OVER (PARTITION BY TensorAtomId ORDER BY ValidFrom) AS ScoreDrift
FROM dbo.TensorAtoms
FOR SYSTEM_TIME ALL
WHERE TensorAtomId = 123
ORDER BY ValidFrom;
';
GO

-- =============================================
-- Comparison: Temporal Tables vs VectorDriftOverTime CLR
-- =============================================

PRINT '';
PRINT '--- Temporal Tables vs VectorDriftOverTime CLR Aggregate ---';
PRINT '';
PRINT 'VectorDriftOverTime CLR Aggregate:';
PRINT '  Pros:';
PRINT '    • Custom drift calculation logic';
PRINT '    • Vector-specific distance metrics';
PRINT '    • Memory-efficient (streaming aggregate)';
PRINT '  Cons:';
PRINT '    • Requires CLR maintenance';
PRINT '    • Must manually track history';
PRINT '    • No point-in-time queries';
PRINT '    • No automatic cleanup';
PRINT '';
PRINT 'Temporal Tables (SYSTEM_VERSIONING):';
PRINT '  Pros:';
PRINT '    • Zero code changes (automatic tracking)';
PRINT '    • Standard SQL queries (FOR SYSTEM_TIME)';
PRINT '    • Built-in retention policies';
PRINT '    • Instant rollback capability';
PRINT '    • Regulatory compliance (audit trail)';
PRINT '  Cons:';
PRINT '    • Requires SQL 2016+';
PRINT '    • History table storage overhead';
PRINT '    • Less flexible than custom CLR';
PRINT '';
PRINT 'Recommendation: Use BOTH';
PRINT '  • Temporal tables for point-in-time queries and rollback';
PRINT '  • VectorDriftOverTime for advanced vector-specific analytics';
GO

-- =============================================
-- Performance Considerations
-- =============================================

PRINT '';
PRINT '--- Performance Considerations ---';
PRINT '';
PRINT 'History Table Size:';
PRINT '  • Grows with every UPDATE/DELETE on current table';
PRINT '  • Clustered index on (ValidTo, ValidFrom) is optimal';
PRINT '  • Columnstore index for analytical queries';
PRINT '  • Retention policy auto-cleans old data (SQL 2017+)';
PRINT '';
PRINT 'Query Performance:';
PRINT '  • FOR SYSTEM_TIME AS OF: Fast (index seek)';
PRINT '  • FOR SYSTEM_TIME ALL: Can be slow (table scan)';
PRINT '  • Use WHERE ValidFrom >= @date for range queries';
PRINT '';
PRINT 'Storage Optimization:';
PRINT '  • PAGE compression on history table';
PRINT '  • Columnstore for high-volume changes';
PRINT '  • Partition sliding window for large histories';
GO

-- =============================================
-- Sample Queries: Model Drift Analysis
-- =============================================

PRINT '';
PRINT '--- Sample Queries: Model Drift Analysis ---';
PRINT '
-- 1. Compare current model to version from 30 days ago
SELECT 
    curr.LayerId,
    curr.ImportanceScore AS CurrentScore,
    prev.ImportanceScore AS ScoreMonthAgo,
    curr.ImportanceScore - prev.ImportanceScore AS Drift
FROM dbo.ModelLayers AS curr
LEFT JOIN dbo.ModelLayers FOR SYSTEM_TIME AS OF DATEADD(DAY, -30, SYSUTCDATETIME()) AS prev
    ON curr.LayerId = prev.LayerId
WHERE curr.ModelId = 1
ORDER BY ABS(curr.ImportanceScore - prev.ImportanceScore) DESC;

-- 2. Track all weight updates for a specific tensor
SELECT 
    ValidFrom,
    ValidTo,
    ImportanceScore,
    DATEDIFF(MINUTE, ValidFrom, ValidTo) AS MinutesActive
FROM dbo.TensorAtoms
FOR SYSTEM_TIME ALL
WHERE TensorAtomId = 456
ORDER BY ValidFrom;

-- 3. Rollback model to previous state
BEGIN TRANSACTION;

-- Disable temporal tracking temporarily
ALTER TABLE dbo.ModelLayers SET (SYSTEM_VERSIONING = OFF);

-- Restore from history
DELETE FROM dbo.ModelLayers WHERE ModelId = 1;

INSERT INTO dbo.ModelLayers (LayerId, ModelId, LayerType, ImportanceScore, ...)
SELECT LayerId, ModelId, LayerType, ImportanceScore, ...
FROM dbo.ModelLayersHistory
FOR SYSTEM_TIME AS OF ''2024-01-01 00:00:00''
WHERE ModelId = 1;

-- Re-enable temporal tracking
ALTER TABLE dbo.ModelLayers SET (SYSTEM_VERSIONING = ON (
    HISTORY_TABLE = dbo.ModelLayersHistory
));

COMMIT;
';
GO

-- =============================================
-- Implementation Checklist
-- =============================================

PRINT '';
PRINT '--- Implementation Checklist ---';
PRINT '';
PRINT '☐ 1. Backup current database';
PRINT '☐ 2. Add ValidFrom/ValidTo columns to ModelLayers';
PRINT '☐ 3. Add ValidFrom/ValidTo columns to TensorAtoms';
PRINT '☐ 4. Enable SYSTEM_VERSIONING on ModelLayers';
PRINT '☐ 5. Enable SYSTEM_VERSIONING on TensorAtoms';
PRINT '☐ 6. Create clustered index on history tables (ValidTo, ValidFrom)';
PRINT '☐ 7. Set retention policy (2 years for ModelLayers, 1 year for TensorAtoms)';
PRINT '☐ 8. Test point-in-time queries';
PRINT '☐ 9. Test rollback procedure';
PRINT '☐ 10. Update sp_UpdateModelWeightsFromFeedback to leverage temporal queries';
PRINT '☐ 11. Monitor history table growth';
PRINT '☐ 12. Document drift analysis queries for team';
GO

-- =============================================
-- Decision Matrix
-- =============================================

PRINT '';
PRINT '=======================================================';
PRINT 'DECISION: Temporal Tables for Model History';
PRINT '';
PRINT 'RECOMMENDED APPROACH:';
PRINT '  • Enable temporal tables on ModelLayers and TensorAtoms';
PRINT '  • Keep VectorDriftOverTime CLR for advanced analytics';
PRINT '  • Use temporal queries for rollback and point-in-time analysis';
PRINT '  • Set 1-2 year retention policies to manage storage';
PRINT '';
PRINT 'BENEFITS:';
PRINT '  ✓ Instant rollback capability (critical for production)';
PRINT '  ✓ Automatic audit trail (compliance requirement)';
PRINT '  ✓ Simplified drift analysis (standard SQL)';
PRINT '  ✓ Zero application code changes';
PRINT '';
PRINT 'RISKS:';
PRINT '  ⚠ History table storage growth (mitigated by retention policy)';
PRINT '  ⚠ Requires SQL 2016+ (already using SQL 2025)';
PRINT '';
PRINT 'NEXT ACTION:';
PRINT '  Implement temporal tables in DEV environment, monitor for 2 weeks,';
PRINT '  then roll out to PROD if no issues detected.';
PRINT '=======================================================';
GO

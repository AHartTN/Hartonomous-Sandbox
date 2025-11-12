USE [$(DatabaseName)]
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '=======================================================';
PRINT 'COLUMNSTORE AND COMPRESSION DEPLOYMENT';
PRINT '=======================================================';
PRINT '';

-- =============================================
-- PART 1: COLUMNSTORE FOR ANALYTICAL TABLES
-- =============================================

-- BillingUsageLedger: Append-only analytical queries
-- Use nonclustered columnstore for analytics while keeping rowstore for OLTP
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'NCCI_BillingUsageLedger_Analytics' AND object_id = OBJECT_ID('dbo.BillingUsageLedger'))
BEGIN
    CREATE NONCLUSTERED COLUMNSTORE INDEX NCCI_BillingUsageLedger_Analytics
    ON dbo.BillingUsageLedger
    (
        TenantId,
        PrincipalId,
        Operation,
        MessageType,
        Handler,
        Units,
        BaseRate,
        Multiplier,
        TotalCost,
        TimestampUtc
    );

    PRINT '✓ Created columnstore index on BillingUsageLedger for analytics';
END
ELSE
    PRINT '○ Columnstore index already exists on BillingUsageLedger';
GO

-- TensorAtomCoefficients: CRITICAL for SVD query performance
-- Analytical queries on tensor coefficients (SVD-as-GEOMETRY queries)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'NCCI_TensorAtomCoefficients_SVD' AND object_id = OBJECT_ID('dbo.TensorAtomCoefficients'))
BEGIN
    CREATE NONCLUSTERED COLUMNSTORE INDEX NCCI_TensorAtomCoefficients_SVD
    ON dbo.TensorAtomCoefficients
    (
        ParentLayerId,
        TensorAtomId,
        Coefficient,
        Rank
    );

    PRINT '✓ Created columnstore index on TensorAtomCoefficients for SVD queries (CRITICAL)';
END
ELSE
    PRINT '○ Columnstore index already exists on TensorAtomCoefficients';
GO

-- AutonomousImprovementHistory: Analytical queries on improvement patterns
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'NCCI_AutonomousImprovementHistory_Analytics' AND object_id = OBJECT_ID('dbo.AutonomousImprovementHistory'))
BEGIN
    CREATE NONCLUSTERED COLUMNSTORE INDEX NCCI_AutonomousImprovementHistory_Analytics
    ON dbo.AutonomousImprovementHistory
    (
        ChangeType,
        RiskLevel,
        EstimatedImpact,
        SuccessScore,
        TestsPassed,
        TestsFailed,
        PerformanceDelta,
        WasDeployed,
        WasRolledBack,
        StartedAt,
        CompletedAt
    );
    
    PRINT '✓ Created columnstore index on AutonomousImprovementHistory for pattern analysis';
END
ELSE
    PRINT '○ Columnstore index already exists on AutonomousImprovementHistory';
GO

-- =============================================
-- PART 2: ROW/PAGE COMPRESSION FOR OLTP TABLES
-- =============================================

-- BillingUsageLedger: ROW compression (better for insert performance)
IF NOT EXISTS (SELECT 1 FROM sys.partitions WHERE object_id = OBJECT_ID('dbo.BillingUsageLedger') AND data_compression > 0)
BEGIN
    ALTER TABLE dbo.BillingUsageLedger REBUILD WITH (DATA_COMPRESSION = ROW);
    PRINT '✓ Applied ROW compression to BillingUsageLedger';
END
ELSE
    PRINT '○ BillingUsageLedger already compressed';
GO

-- AutonomousImprovementHistory: PAGE compression (better compression ratio, slightly slower inserts acceptable)
IF NOT EXISTS (SELECT 1 FROM sys.partitions WHERE object_id = OBJECT_ID('dbo.AutonomousImprovementHistory') AND data_compression = 2)
BEGIN
    ALTER TABLE dbo.AutonomousImprovementHistory REBUILD WITH (DATA_COMPRESSION = PAGE);
    PRINT '✓ Applied PAGE compression to AutonomousImprovementHistory';
END
ELSE
    PRINT '○ AutonomousImprovementHistory already compressed';
GO

-- =============================================
-- PART 3: COMPRESSION ANALYSIS QUERY
-- =============================================

PRINT '';
PRINT 'Compression analysis:';
PRINT '';

SELECT 
    OBJECT_SCHEMA_NAME(p.object_id) + '.' + OBJECT_NAME(p.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    p.data_compression_desc AS CompressionType,
    p.rows AS [RowCount],
    CAST(SUM(a.used_pages) * 8 / 1024.0 AS DECIMAL(10,2)) AS UsedSpaceMB,
    CAST(SUM(a.total_pages) * 8 / 1024.0 AS DECIMAL(10,2)) AS AllocatedSpaceMB
FROM sys.partitions p
INNER JOIN sys.indexes i ON p.object_id = i.object_id AND p.index_id = i.index_id
INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
WHERE OBJECT_SCHEMA_NAME(p.object_id) IN ('dbo', 'graph')
    AND OBJECT_NAME(p.object_id) IN (
        'BillingUsageLedger',
        'BillingUsageLedger_InMemory',
        'AutonomousImprovementHistory',
        'AtomGraphNodes',
        'AtomGraphEdges'
    )
GROUP BY p.object_id, i.name, i.type_desc, p.data_compression_desc, p.rows
ORDER BY TableName, IndexName;

PRINT '';
PRINT '=======================================================';
PRINT 'Columnstore and compression deployment complete!';
PRINT 'Benefits:';
PRINT '  - 5-10x compression for analytical data';
PRINT '  - Batch mode execution for columnar scans';
PRINT '  - Reduced I/O for aggregate queries';
PRINT '  - 20-40% space savings with row/page compression';
PRINT '=======================================================';
GO
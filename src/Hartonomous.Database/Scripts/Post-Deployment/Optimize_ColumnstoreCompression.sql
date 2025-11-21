/*
================================================================================
Table Compression Optimization
================================================================================
Purpose: Applies ROW/PAGE compression to tables for storage optimization

Requirements:
- Runs AFTER DACPAC deployment (tables must exist)
- ROW compression: Better for insert-heavy OLTP workloads
- PAGE compression: Better compression ratio for append-mostly tables

Note:
- Columnstore indexes are now in DACPAC model (Indexes/ folder)
- This script only handles data compression via ALTER TABLE REBUILD
================================================================================
*/

SET NOCOUNT ON;
PRINT '=======================================================';
PRINT 'TABLE COMPRESSION OPTIMIZATION';
PRINT '=======================================================';
PRINT '';

-- BillingUsageLedger: ROW compression (better for insert performance)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.partitions 
    WHERE object_id = OBJECT_ID('dbo.BillingUsageLedger') 
      AND data_compression > 0
)
BEGIN
    ALTER TABLE dbo.BillingUsageLedger REBUILD WITH (DATA_COMPRESSION = ROW);
    PRINT '✓ Applied ROW compression to BillingUsageLedger';
END
ELSE
    PRINT '○ BillingUsageLedger already compressed';
GO

-- AutonomousImprovementHistory: PAGE compression (better compression ratio)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.partitions 
    WHERE object_id = OBJECT_ID('dbo.AutonomousImprovementHistory') 
      AND data_compression = 2
)
BEGIN
    ALTER TABLE dbo.AutonomousImprovementHistory REBUILD WITH (DATA_COMPRESSION = PAGE);
    PRINT '✓ Applied PAGE compression to AutonomousImprovementHistory';
END
ELSE
    PRINT '○ AutonomousImprovementHistory already compressed';
GO

-- AtomComposition: CLUSTERED COLUMNSTORE (CRITICAL for pixel-level atomization)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE object_id = OBJECT_ID('dbo.AtomComposition') 
      AND type = 5  -- Clustered Columnstore
)
BEGIN
    -- Drop existing clustered index first
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.AtomComposition') AND name = 'PK_AtomComposition')
    BEGIN
        ALTER TABLE dbo.AtomComposition DROP CONSTRAINT PK_AtomComposition;
        PRINT '✓ Dropped PK_AtomComposition';
    END

    -- Create Clustered Columnstore Index with ARCHIVE compression (95%+ compression)
    CREATE CLUSTERED COLUMNSTORE INDEX CCI_AtomComposition
    ON dbo.AtomComposition
    WITH (DATA_COMPRESSION = COLUMNSTORE_ARCHIVE, MAXDOP = 0);
    
    PRINT '✓ Applied CLUSTERED COLUMNSTORE to AtomComposition (ARCHIVE compression)';
    PRINT '  Expected: 95%+ compression ratio for pixel/token sequences';
END
ELSE
    PRINT '○ AtomComposition already has Clustered Columnstore';
GO

PRINT '✓ Table compression optimization complete';
PRINT '';
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
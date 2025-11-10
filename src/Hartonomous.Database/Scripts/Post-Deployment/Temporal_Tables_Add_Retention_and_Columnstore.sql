-- =============================================
-- Optimize Temporal Table History Retention
-- Add 90-day retention + columnstore history indexes
-- MS Docs: https://learn.microsoft.com/en-us/sql/relational-databases/tables/manage-retention-of-historical-data-in-system-versioned-temporal-tables
-- =============================================

-- =============================================
-- PART 1: TensorAtomCoefficients - Add 90-day retention
-- =============================================

-- Step 1: Disable system versioning temporarily
IF EXISTS (
    SELECT 1 FROM sys.tables 
    WHERE name = 'TensorAtomCoefficients' 
    AND temporal_type = 2
)
BEGIN
    ALTER TABLE dbo.TensorAtomCoefficients SET (SYSTEM_VERSIONING = OFF);
    END;

-- Step 2: Re-enable with 90-day retention
ALTER TABLE dbo.TensorAtomCoefficients
SET (
    SYSTEM_VERSIONING = ON (
        HISTORY_TABLE = dbo.TensorAtomCoefficients_History,
        HISTORY_RETENTION_PERIOD = 90 DAYS
    )
);

-- Step 3: Convert history table to clustered columnstore
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'CCI_TensorAtomCoefficients_History' 
    AND object_id = OBJECT_ID('dbo.TensorAtomCoefficients_History')
)
BEGIN
    -- Drop existing nonclustered index first
    IF EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_TensorAtomCoefficients_History_Period' 
        AND object_id = OBJECT_ID('dbo.TensorAtomCoefficients_History')
    )
    BEGIN
        DROP INDEX IX_TensorAtomCoefficients_History_Period ON dbo.TensorAtomCoefficients_History;
    END;

    -- Create clustered columnstore index (optimal for large historical data)
    CREATE CLUSTERED COLUMNSTORE INDEX CCI_TensorAtomCoefficients_History
    ON dbo.TensorAtomCoefficients_History;

    END
ELSE
BEGIN
    END;

-- =============================================
-- PART 2: Weights - Add 90-day retention
-- =============================================

-- Step 1: Disable system versioning temporarily
IF EXISTS (
    SELECT 1 FROM sys.tables 
    WHERE name = 'Weights' 
    AND temporal_type = 2
)
BEGIN
    ALTER TABLE dbo.Weights SET (SYSTEM_VERSIONING = OFF);
    END;

-- Step 2: Re-enable with 90-day retention
ALTER TABLE dbo.Weights
SET (
    SYSTEM_VERSIONING = ON (
        HISTORY_TABLE = dbo.Weights_History,
        HISTORY_RETENTION_PERIOD = 90 DAYS
    )
);

-- Step 3: Convert history table to clustered columnstore
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'CCI_Weights_History' 
    AND object_id = OBJECT_ID('dbo.Weights_History')
)
BEGIN
    -- Create clustered columnstore index (optimal for large historical data)
    CREATE CLUSTERED COLUMNSTORE INDEX CCI_Weights_History
    ON dbo.Weights_History;

    END
ELSE
BEGIN
    END;

-- =============================================
-- VERIFICATION
-- =============================================
SELECT 
    t.name AS TableName,
    CASE t.temporal_type
        WHEN 2 THEN 'System-Versioned Temporal'
        ELSE 'Non-Temporal'
    END AS TableType,
    OBJECT_NAME(t.history_table_id) AS HistoryTable,
    t.history_retention_period AS RetentionPeriod,
    t.history_retention_period_unit_desc AS RetentionUnit,
    i.type_desc AS HistoryIndexType,
    i.name AS HistoryIndexName
FROM sys.tables t
LEFT JOIN sys.indexes i ON i.object_id = t.history_table_id AND i.index_id = 1
WHERE t.name IN ('TensorAtomCoefficients', 'Weights')
ORDER BY t.name;

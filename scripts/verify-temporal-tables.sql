-- Verify temporal tables implementation (L0.1.3)

-- 1. Check which tables are temporal
SELECT 
    SCHEMA_NAME(t.schema_id) AS SchemaName,
    t.name AS TableName,
    t.temporal_type_desc AS TemporalType,
    OBJECT_NAME(t.history_table_id) AS HistoryTableName
FROM sys.tables t
WHERE t.temporal_type = 2 -- 2 = SYSTEM_VERSIONED_TEMPORAL_TABLE
ORDER BY t.name;

-- 2. Check temporal columns on a sample table (Atoms)
SELECT 
    c.name AS ColumnName,
    TYPE_NAME(c.user_type_id) AS DataType,
    c.generated_always_type_desc AS GeneratedType,
    c.is_hidden AS IsHidden
FROM sys.columns c
WHERE c.object_id = OBJECT_ID('dbo.Atoms')
  AND c.name IN ('ValidFrom', 'ValidTo')
ORDER BY c.column_id;

-- 3. Check retention policies
SELECT 
    SCHEMA_NAME(t.schema_id) AS SchemaName,
    t.name AS TableName,
    t.history_retention_period AS RetentionValue,
    t.history_retention_period_unit_desc AS RetentionUnit
FROM sys.tables t
WHERE t.temporal_type = 2
ORDER BY t.name;

-- 4. Verify we can query point-in-time data (should return 0 rows for new tables)
SELECT COUNT(*) AS CurrentRowCount FROM dbo.Atoms;
SELECT COUNT(*) AS HistoricalRowCount FROM dbo.AtomsHistory;

-- 5. Test FOR SYSTEM_TIME query syntax
SELECT TOP 1 
    AtomId, 
    CanonicalText,
    ValidFrom,
    ValidTo
FROM dbo.Atoms FOR SYSTEM_TIME ALL
WHERE ValidFrom IS NOT NULL;

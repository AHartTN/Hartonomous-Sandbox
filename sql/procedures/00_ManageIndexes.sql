-- UNIFIED INDEX MANAGEMENT
USE Hartonomous;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ManageHartonomousIndexes
    @Operation NVARCHAR(20) = 'ANALYZE', -- CREATE, REBUILD, ANALYZE, OPTIMIZE, VALIDATE
    @TableName NVARCHAR(128) = NULL       -- NULL = all tables
AS
BEGIN
    SET NOCOUNT ON;

    IF @Operation = 'CREATE'
    BEGIN
        PRINT 'Creating spatial indexes (VECTOR indexes commented - preview limitation)...';
        EXEC('EXEC sp_executesql N''EXEC $(DBPath)\sql\procedures\00_CreateSpatialIndexes.sql''');
        RETURN;
    END

    IF @Operation = 'REBUILD'
    BEGIN
        PRINT 'Rebuilding fragmented indexes...';
        
        DECLARE @sql NVARCHAR(MAX);
        DECLARE rebuild_cursor CURSOR FOR
            SELECT 
                'ALTER INDEX [' + i.name + '] ON [' + OBJECT_SCHEMA_NAME(i.object_id) + '].[' + OBJECT_NAME(i.object_id) + '] REBUILD WITH (ONLINE = OFF, SORT_IN_TEMPDB = ON);'
            FROM sys.indexes i
            INNER JOIN sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ps
                ON i.object_id = ps.object_id AND i.index_id = ps.index_id
            WHERE ps.avg_fragmentation_in_percent > 30
              AND i.type_desc IN ('CLUSTERED', 'NONCLUSTERED', 'SPATIAL')
              AND (@TableName IS NULL OR OBJECT_NAME(i.object_id) = @TableName);

        OPEN rebuild_cursor;
        FETCH NEXT FROM rebuild_cursor INTO @sql;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            PRINT @sql;
            EXEC sp_executesql @sql;
            FETCH NEXT FROM rebuild_cursor INTO @sql;
        END;

        CLOSE rebuild_cursor;
        DEALLOCATE rebuild_cursor;
        
        PRINT 'Index rebuild complete.';
        RETURN;
    END

    IF @Operation = 'ANALYZE'
    BEGIN
        PRINT 'Index usage statistics:';
        
        SELECT
            OBJECT_NAME(i.object_id) AS TableName,
            i.name AS IndexName,
            i.type_desc AS IndexType,
            us.user_seeks,
            us.user_scans,
            us.user_lookups,
            us.user_updates,
            CASE 
                WHEN us.user_seeks + us.user_scans + us.user_lookups = 0 THEN 0
                ELSE CAST(us.user_seeks AS FLOAT) / (us.user_seeks + us.user_scans + us.user_lookups)
            END AS SeekRatio,
            ps.avg_fragmentation_in_percent,
            ps.page_count
        FROM sys.indexes i
        LEFT JOIN sys.dm_db_index_usage_stats us
            ON i.object_id = us.object_id AND i.index_id = us.index_id AND us.database_id = DB_ID()
        LEFT JOIN sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ps
            ON i.object_id = ps.object_id AND i.index_id = ps.index_id
        WHERE i.object_id IN (
            SELECT object_id FROM sys.tables 
            WHERE name IN ('Atoms', 'AtomEmbeddings', 'TensorAtoms', 'Models', 'ModelLayers', 'InferenceRequests')
        )
        AND i.type_desc IN ('CLUSTERED', 'NONCLUSTERED', 'SPATIAL')
        AND (@TableName IS NULL OR OBJECT_NAME(i.object_id) = @TableName)
        ORDER BY OBJECT_NAME(i.object_id), i.name;
        
        RETURN;
    END

    IF @Operation = 'OPTIMIZE'
    BEGIN
        PRINT 'Optimizing statistics...';
        
        UPDATE STATISTICS dbo.Atoms WITH FULLSCAN;
        UPDATE STATISTICS dbo.AtomEmbeddings WITH FULLSCAN;
        UPDATE STATISTICS dbo.TensorAtoms WITH FULLSCAN;
        UPDATE STATISTICS dbo.Models WITH FULLSCAN;
        UPDATE STATISTICS dbo.ModelLayers WITH FULLSCAN;
        UPDATE STATISTICS dbo.InferenceRequests WITH FULLSCAN;
        
        PRINT 'Statistics updated.';
        RETURN;
    END

    IF @Operation = 'VALIDATE'
    BEGIN
        PRINT 'Validating index health...';
        
        -- Check for missing indexes
        SELECT
            OBJECT_NAME(d.object_id) AS TableName,
            'CREATE INDEX IX_' + OBJECT_NAME(d.object_id) + '_' + REPLACE(REPLACE(d.equality_columns, '[', ''), ']', '') AS SuggestedIndex,
            d.equality_columns,
            d.inequality_columns,
            d.included_columns,
            s.avg_user_impact,
            s.user_seeks,
            s.user_scans
        FROM sys.dm_db_missing_index_details d
        INNER JOIN sys.dm_db_missing_index_groups g ON d.index_handle = g.index_handle
        INNER JOIN sys.dm_db_missing_index_group_stats s ON g.index_group_handle = s.group_handle
        WHERE d.database_id = DB_ID()
          AND s.avg_user_impact > 50
          AND (@TableName IS NULL OR OBJECT_NAME(d.object_id) = @TableName)
        ORDER BY s.avg_user_impact DESC;
        
        PRINT 'Validation complete.';
        RETURN;
    END

    RAISERROR('Invalid operation. Use: CREATE, REBUILD, ANALYZE, OPTIMIZE, VALIDATE', 16, 1);
END;
GO

PRINT 'Index management procedure created.';
GO

CREATE PROCEDURE dbo.sp_ManageHartonomousIndexes
    @Operation NVARCHAR(20) = 'ANALYZE', -- CREATE, REBUILD, ANALYZE, OPTIMIZE, VALIDATE
    @TableName NVARCHAR(128) = NULL       -- NULL = all tables
AS
BEGIN
    SET NOCOUNT ON;

    IF @Operation = 'CREATE'
    BEGIN
        PRINT 'Creating spatial indexes via Common.CreateSpatialIndexes.sql...';
        PRINT 'Invoke Common.CreateSpatialIndexes.sql through the deployment pipeline to materialize spatial indexes.';
        RETURN;
    END;

    IF @Operation = 'REBUILD'
    BEGIN
        PRINT 'Rebuilding indexes for targeted tables...';

        DECLARE @indexes TABLE (
            SchemaName NVARCHAR(128),
            TableName NVARCHAR(128),
            IndexName NVARCHAR(128)
        );

        INSERT INTO @indexes (SchemaName, TableName, IndexName)
        SELECT
            OBJECT_SCHEMA_NAME(i.object_id),
            OBJECT_NAME(i.object_id),
            i.name
        FROM sys.indexes i
        INNER JOIN sys.tables t ON i.object_id = t.object_id
        WHERE i.name IS NOT NULL
          AND i.is_hypothetical = 0
          AND i.is_disabled = 0
          AND i.type_desc IN ('CLUSTERED', 'NONCLUSTERED', 'SPATIAL')
          AND (@TableName IS NULL OR t.name = @TableName);

        DECLARE @schema NVARCHAR(128);
        DECLARE @table NVARCHAR(128);
        DECLARE @index NVARCHAR(128);
        DECLARE @command NVARCHAR(MAX);
        DECLARE @rebuilt INT = 0;

        DECLARE rebuild_cursor CURSOR FAST_FORWARD FOR
            SELECT SchemaName, TableName, IndexName FROM @indexes ORDER BY SchemaName, TableName, IndexName;

        OPEN rebuild_cursor;
        FETCH NEXT FROM rebuild_cursor INTO @schema, @table, @index;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @command = N'ALTER INDEX ' + QUOTENAME(@index) + N' ON ' + QUOTENAME(@schema) + N'.' + QUOTENAME(@table) + N' REBUILD WITH (ONLINE = OFF, SORT_IN_TEMPDB = ON);';
            PRINT @command;
            EXEC sp_executesql @command;
            SET @rebuilt += 1;
            FETCH NEXT FROM rebuild_cursor INTO @schema, @table, @index;
        END;

        CLOSE rebuild_cursor;
        DEALLOCATE rebuild_cursor;

        PRINT CONCAT('Index rebuild complete. Total indexes rebuilt: ', @rebuilt);
        RETURN;
    END;

    IF @Operation = 'ANALYZE'
    BEGIN
        PRINT 'Index usage statistics:';

        SELECT
            OBJECT_SCHEMA_NAME(i.object_id) AS SchemaName,
            OBJECT_NAME(i.object_id) AS TableName,
            i.name AS IndexName,
            i.type_desc AS IndexType,
            us.user_seeks,
            us.user_scans,
            us.user_lookups,
            us.user_updates,
            ps.row_count,
            ps.reserved_page_count,
            ps.used_page_count
        FROM sys.indexes i
        INNER JOIN sys.tables t ON i.object_id = t.object_id
        LEFT JOIN sys.dm_db_index_usage_stats us
            ON i.object_id = us.object_id AND i.index_id = us.index_id AND us.database_id = DB_ID()
        LEFT JOIN sys.dm_db_partition_stats ps
            ON i.object_id = ps.object_id AND i.index_id = ps.index_id
        WHERE i.name IS NOT NULL
          AND i.is_hypothetical = 0
          AND i.type_desc IN ('CLUSTERED', 'NONCLUSTERED', 'SPATIAL')
          AND (@TableName IS NULL OR t.name = @TableName)
        ORDER BY SchemaName, TableName, IndexName;

        RETURN;
    END;

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
    END;

    IF @Operation = 'VALIDATE'
    BEGIN
        PRINT 'Validating index health...';

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
    END;

    RAISERROR('Invalid operation. Use: CREATE, REBUILD, ANALYZE, OPTIMIZE, VALIDATE', 16, 1);
END;
GO
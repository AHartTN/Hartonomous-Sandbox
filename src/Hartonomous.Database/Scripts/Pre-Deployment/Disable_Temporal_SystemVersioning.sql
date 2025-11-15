USE [$(DatabaseName)]
GO

/*
    Temporarily disable system-versioning (and finite retention) before DAC deployment
    so schema compare can rebuild clustered columnstore indexes without hitting
    Msg 13766 (cannot drop clustered index with retention). See Microsoft docs:
    https://learn.microsoft.com/en-us/sql/relational-databases/tables/manage-retention-of-historical-data-in-system-versioned-temporal-tables
*/
PRINT 'Pre-Deployment: checking temporal tables that require SYSTEM_VERSIONING = OFF...';

DECLARE @tables TABLE (QualifiedName NVARCHAR(513));

INSERT INTO @tables (QualifiedName)
SELECT QUOTENAME(SCHEMA_NAME(t.schema_id)) + '.' + QUOTENAME(t.name)
FROM sys.tables t
WHERE t.temporal_type = 2
  AND t.name IN (N'TensorAtomCoefficients', N'Weights');

DECLARE @target NVARCHAR(513);
DECLARE @sql NVARCHAR(MAX);

DECLARE temporal_cursor CURSOR FAST_FORWARD FOR
SELECT QualifiedName FROM @tables;

OPEN temporal_cursor;
FETCH NEXT FROM temporal_cursor INTO @target;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'ALTER TABLE ' + @target + N' SET (SYSTEM_VERSIONING = OFF);';

    BEGIN TRY
        EXEC sp_executesql @sql;
        PRINT '  SYSTEM_VERSIONING disabled for ' + @target;
    END TRY
    BEGIN CATCH
        DECLARE @err NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR('Failed to disable SYSTEM_VERSIONING for %s. %s', 16, 1, @target, @err);
    END CATCH;

    FETCH NEXT FROM temporal_cursor INTO @target;
END

CLOSE temporal_cursor;
DEALLOCATE temporal_cursor;
GO

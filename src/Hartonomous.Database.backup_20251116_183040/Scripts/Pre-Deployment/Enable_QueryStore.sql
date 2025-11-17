USE [$(DatabaseName)]
GO

IF EXISTS (SELECT 1 FROM sys.databases WHERE name = DB_NAME() AND is_query_store_on = 0)
BEGIN
    ALTER DATABASE [$(DatabaseName)]
    SET QUERY_STORE = ON
    (
        OPERATION_MODE = READ_WRITE,
        CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30),
        DATA_FLUSH_INTERVAL_SECONDS = 900,
        INTERVAL_LENGTH_MINUTES = 60,
        MAX_STORAGE_SIZE_MB = 1000,
        QUERY_CAPTURE_MODE = AUTO,
        SIZE_BASED_CLEANUP_MODE = AUTO,
        MAX_PLANS_PER_QUERY = 200
    );
    PRINT 'Query Store enabled successfully for [$(DatabaseName)] database.';
END
ELSE IF EXISTS (SELECT 1 FROM sys.databases WHERE name = DB_NAME() AND is_query_store_on = 1)
BEGIN
    PRINT 'Query Store is already enabled for [$(DatabaseName)] database.';
END
ELSE
BEGIN
    PRINT 'Could not determine Query Store status or database does not exist.';
END
GO
-- =============================================
-- Enable Query Store for query performance monitoring
-- =============================================
-- Query Store provides:
-- - Automatic query plan history
-- - Regression detection
-- - Plan forcing capabilities
-- - Wait statistics tracking
-- =============================================

USE master;
GO

ALTER DATABASE Hartonomous
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
GO

PRINT 'Query Store enabled successfully for Hartonomous database';
PRINT 'Access Query Store reports in SSMS: Database > Query Store folder';
GO

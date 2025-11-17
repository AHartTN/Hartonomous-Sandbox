/*
================================================================================
Optional Feature: Query Store
================================================================================
Purpose:
  Enable Query Store for automatic query performance tracking and analysis.
  Captures query execution plans, runtime statistics, and wait statistics.

Benefits:
  - Identify regressed queries automatically
  - Force specific execution plans
  - Analyze query performance over time
  - Troubleshoot parameter sniffing issues

Usage:
  Uncomment this line in Script.PreDeployment.sql to enable:
  -- :r .\Enable_QueryStore.sql

Configuration:
  - OPERATION_MODE: READ_WRITE (captures query data)
  - STALE_QUERY_THRESHOLD_DAYS: 30 (cleanup old queries)
  - DATA_FLUSH_INTERVAL_SECONDS: 900 (15 min flush to disk)
  - INTERVAL_LENGTH_MINUTES: 60 (1 hour aggregation)
  - MAX_STORAGE_SIZE_MB: 1000 (1 GB max storage)
  - QUERY_CAPTURE_MODE: AUTO (intelligent query capture)

Requirements:
  - SQL Server 2016+ (all editions)
  - Minimal performance overhead (<3%)

Idempotency:
  - Safe to run multiple times
  - Checks if Query Store already enabled
================================================================================
*/

USE [$(DatabaseName)]
GO

PRINT 'Optional Feature: Enabling Query Store...';
PRINT '';
GO

IF EXISTS (SELECT 1 FROM sys.databases WHERE name = DB_NAME() AND is_query_store_on = 0)
BEGIN
    PRINT '  Configuring Query Store with recommended settings...';
    
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
    
    PRINT '  ✓ Query Store enabled successfully';
END
ELSE IF EXISTS (SELECT 1 FROM sys.databases WHERE name = DB_NAME() AND is_query_store_on = 1)
BEGIN
    PRINT '  ○ Query Store already enabled';
END
ELSE
BEGIN
    PRINT '  ✗ Error: Could not determine Query Store status';
END

PRINT '';
PRINT '✓ Query Store configuration complete';
PRINT '';
GO
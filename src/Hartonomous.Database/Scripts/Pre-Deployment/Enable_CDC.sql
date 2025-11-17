/*
================================================================================
Optional Feature: Change Data Capture (CDC)
================================================================================
Purpose:
  Enable CDC on database and specific tables to track all data changes
  (INSERT, UPDATE, DELETE) for auditing and synchronization.

Targeted Tables:
  - dbo.Atoms (primary entity table)
  - dbo.Models (ML model metadata)
  - dbo.InferenceRequests (inference tracking)

Usage:
  Uncomment this line in Script.PreDeployment.sql to enable:
  -- :r .\Enable_CDC.sql

Requirements:
  - SQL Server 2008+ (Standard/Enterprise)
  - Requires db_owner permissions
  - Creates 'cdc' schema and system tables
  - Enables SQL Server Agent jobs for cleanup

Idempotency:
  - Safe to run multiple times
  - Checks if CDC already enabled before configuring
================================================================================
*/

USE [$(DatabaseName)]
GO

PRINT 'Optional Feature: Enabling Change Data Capture (CDC)...';
PRINT '';
GO

-- ============================================================================
-- Step 1: Enable CDC on Database
-- ============================================================================
PRINT '  [1/4] Configuring CDC at database level...';

IF (SELECT is_cdc_enabled FROM sys.databases WHERE name = DB_NAME()) = 0
BEGIN
    EXEC sys.sp_cdc_enable_db;
    PRINT '        ✓ CDC enabled for database [$(DatabaseName)]';
END
ELSE
    PRINT '        ○ CDC already enabled for database [$(DatabaseName)]';

PRINT '';
GO

-- ============================================================================
-- Step 2: Enable CDC on Atoms Table
-- ============================================================================
PRINT '  [2/4] Configuring CDC for dbo.Atoms...';

IF NOT EXISTS (
    SELECT 1 FROM cdc.change_tables 
    WHERE source_schema = N'dbo' 
      AND source_name = N'Atoms'
)
BEGIN
    EXEC sys.sp_cdc_enable_table
        @source_schema = N'dbo',
        @source_name = N'Atoms',
        @role_name = NULL,
        @supports_net_changes = 1;
    PRINT '        ✓ CDC enabled for dbo.Atoms';
END
ELSE
    PRINT '        ○ CDC already enabled for dbo.Atoms';

PRINT '';
GO

-- ============================================================================
-- Step 3: Enable CDC on Models Table
-- ============================================================================
PRINT '  [3/4] Configuring CDC for dbo.Models...';

IF NOT EXISTS (
    SELECT 1 FROM cdc.change_tables 
    WHERE source_schema = N'dbo' 
      AND source_name = N'Models'
)
BEGIN
    EXEC sys.sp_cdc_enable_table
        @source_schema = N'dbo',
        @source_name = N'Models',
        @role_name = NULL,
        @supports_net_changes = 1;
    PRINT '        ✓ CDC enabled for dbo.Models';
END
ELSE
    PRINT '        ○ CDC already enabled for dbo.Models';

PRINT '';
GO

-- ============================================================================
-- Step 4: Enable CDC on InferenceRequests Table
-- ============================================================================
PRINT '  [4/4] Configuring CDC for dbo.InferenceRequests...';

IF NOT EXISTS (
    SELECT 1 FROM cdc.change_tables 
    WHERE source_schema = N'dbo' 
      AND source_name = N'InferenceRequests'
)
BEGIN
    EXEC sys.sp_cdc_enable_table
        @source_schema = N'dbo',
        @source_name = N'InferenceRequests',
        @role_name = NULL,
        @supports_net_changes = 1;
    PRINT '        ✓ CDC enabled for dbo.InferenceRequests';
END
ELSE
    PRINT '        ○ CDC already enabled for dbo.InferenceRequests';

PRINT '';
PRINT '✓ Change Data Capture configuration complete';
PRINT '';
GO
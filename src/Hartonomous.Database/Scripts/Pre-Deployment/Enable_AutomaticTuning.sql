/*
================================================================================
Optional Feature: Automatic Tuning
================================================================================
Purpose:
  Enable Automatic Plan Correction to automatically fix query plan regressions.
  Uses Query Store data to detect and revert to last known good execution plan.

Features:
  - FORCE_LAST_GOOD_PLAN: Auto-revert regressed query plans
  - Continuous monitoring of query performance
  - Automatic performance optimization without DBA intervention

Usage:
  Uncomment this line in Script.PreDeployment.sql to enable:
  -- :r .\Enable_AutomaticTuning.sql

Requirements:
  - SQL Server 2017+ (all editions)
  - Query Store must be enabled (see Enable_QueryStore.sql)
  - Azure SQL Database has additional tuning options available

Behavior:
  - Detects plan regression through Query Store statistics
  - Automatically forces last known good plan
  - Logs tuning actions in sys.dm_db_tuning_recommendations
  - Can be monitored via DMVs and Query Store reports

Idempotency:
  - Safe to run multiple times
  - Checks if automatic tuning already enabled
================================================================================
*/

USE [$(DatabaseName)]
GO

PRINT 'Optional Feature: Enabling Automatic Tuning...';
PRINT '';
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_automatic_tuning_options
    WHERE name = 'FORCE_LAST_GOOD_PLAN' 
      AND actual_state_desc = 'ON'
)
BEGIN
    PRINT '  Configuring FORCE_LAST_GOOD_PLAN = ON...';
    
    ALTER DATABASE [$(DatabaseName)]
    SET AUTOMATIC_TUNING (FORCE_LAST_GOOD_PLAN = ON);
    
    PRINT '  ✓ Automatic Tuning enabled: FORCE_LAST_GOOD_PLAN = ON';
END
ELSE
    PRINT '  ○ Automatic Tuning: FORCE_LAST_GOOD_PLAN already enabled';

PRINT '';
PRINT '✓ Automatic Tuning configuration complete';
PRINT 'Note: Requires Query Store enabled to function';
PRINT '';
GO
-- Enable Automatic Tuning for self-healing query regression fixes
-- Integrates with Query Store to force last good execution plans automatically
-- Part of Hartonomous autonomous loop: sp_Analyze detects regressions via sys.dm_db_tuning_recommendations

USE Hartonomous;
GO

-- Enable Automatic Tuning at database level
ALTER DATABASE Hartonomous
SET AUTOMATIC_TUNING (FORCE_LAST_GOOD_PLAN = ON);
GO

-- Verify configuration
SELECT 
    name,
    desired_state_desc,
    actual_state_desc,
    reason_desc
FROM sys.database_automatic_tuning_options
WHERE name = 'FORCE_LAST_GOOD_PLAN';
GO

PRINT 'Automatic Tuning enabled: FORCE_LAST_GOOD_PLAN = ON';
PRINT 'SQL Server will automatically force last good execution plans when query regressions are detected.';
PRINT 'Recommendations available via sys.dm_db_tuning_recommendations (integrated into sp_Analyze).';
GO

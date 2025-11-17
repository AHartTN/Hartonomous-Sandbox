USE [$(DatabaseName)]
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_automatic_tuning_options
    WHERE name = 'FORCE_LAST_GOOD_PLAN' AND actual_state_desc = 'ON'
)
BEGIN
    ALTER DATABASE [$(DatabaseName)]
    SET AUTOMATIC_TUNING (FORCE_LAST_GOOD_PLAN = ON);
    PRINT 'Automatic Tuning enabled: FORCE_LAST_GOOD_PLAN = ON for [$(DatabaseName)] database.';
END
ELSE
BEGIN
    PRINT 'Automatic Tuning: FORCE_LAST_GOOD_PLAN is already ON for [$(DatabaseName)] database.';
END
GO
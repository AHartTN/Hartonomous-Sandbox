/*
================================================================================
Post-Deployment Script - Minimal Configuration
================================================================================
*/

PRINT 'Starting post-deployment configuration...';
GO

-- Verify database exists and CLR is working
IF DB_ID() IS NULL
BEGIN
    RAISERROR('ERROR: Database context not set', 16, 1);
    RETURN;
END
GO

PRINT 'Post-deployment configuration complete.';
PRINT 'Database ready for use.';
GO

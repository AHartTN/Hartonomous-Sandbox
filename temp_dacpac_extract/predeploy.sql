/*
================================================================================
Pre-Deployment Script - Minimal Setup
================================================================================
*/

PRINT 'Starting pre-deployment setup...';
GO

-- Ensure CLR is enabled (already done via Enable-CLR.sql but verify)
IF NOT EXISTS (SELECT * FROM sys.configurations WHERE name = 'clr enabled' AND value_in_use = 1)
BEGIN
    RAISERROR('ERROR: CLR not enabled. Run scripts\Enable-CLR.sql first', 16, 1);
    RETURN;
END
GO

PRINT 'Pre-deployment setup complete.';
GO

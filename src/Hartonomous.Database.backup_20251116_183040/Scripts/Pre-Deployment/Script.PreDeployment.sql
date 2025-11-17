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

-- Ensure Service Broker is enabled for async message processing
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = DB_NAME() AND is_broker_enabled = 1)
BEGIN
    PRINT 'Enabling Service Broker...';
    DECLARE @sql NVARCHAR(MAX) = 'ALTER DATABASE ' + QUOTENAME(DB_NAME()) + ' SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE';
    EXEC sp_executesql @sql;
    PRINT 'Service Broker enabled.';
END
ELSE
BEGIN
    PRINT 'Service Broker already enabled.';
END
GO

PRINT 'Pre-deployment setup complete.';
GO

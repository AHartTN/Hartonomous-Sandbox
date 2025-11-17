/*
================================================================================
Pre-Deployment Script - CLR and Service Broker Setup
Idempotent: Safe to run multiple times
================================================================================
*/

PRINT 'Starting pre-deployment setup...';
GO

-- ============================================================================
-- Enable CLR Integration (runs every time, idempotent)
-- ============================================================================
USE [master];
GO

DECLARE @clrEnabled INT;
SELECT @clrEnabled = CAST(value_in_use AS INT) FROM sys.configurations WHERE name = 'clr enabled';

IF @clrEnabled = 0
BEGIN
    PRINT 'Enabling CLR integration...';
    EXEC sp_configure 'show advanced options', 1;
    RECONFIGURE;
    EXEC sp_configure 'clr enabled', 1;
    RECONFIGURE;
    PRINT 'CLR enabled.';
END
ELSE
BEGIN
    PRINT 'CLR already enabled.';
END
GO

DECLARE @clrStrictSecurity INT;
SELECT @clrStrictSecurity = CAST(value_in_use AS INT) FROM sys.configurations WHERE name = 'clr strict security';

IF @clrStrictSecurity = 1
BEGIN
    PRINT 'Disabling CLR strict security for UNSAFE assemblies...';
    EXEC sp_configure 'show advanced options', 1;
    RECONFIGURE;
    EXEC sp_configure 'clr strict security', 0;
    RECONFIGURE;
    PRINT 'CLR strict security disabled.';
END
ELSE
BEGIN
    PRINT 'CLR strict security already disabled.';
END
GO

-- Return to target database
USE [$(DatabaseName)];
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

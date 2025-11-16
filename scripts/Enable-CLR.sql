-- Enable CLR and configure for UNSAFE assemblies
-- Run this BEFORE deploying the DACPAC

USE master;
GO

-- Enable CLR integration
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
GO

-- Enable CLR strict security (SQL Server 2017+)
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
GO

EXEC sp_configure 'clr strict security', 0;  -- Disable for UNSAFE assemblies
RECONFIGURE;
GO

PRINT 'CLR enabled successfully';
GO

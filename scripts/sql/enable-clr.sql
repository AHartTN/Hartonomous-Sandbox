-- ============================================================================
-- Enable CLR Integration on SQL Server
-- ============================================================================
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;

EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;

EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;

PRINT 'CLR integration enabled successfully';

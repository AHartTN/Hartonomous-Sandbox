-- ============================================================================
-- Set TRUSTWORTHY ON for CLR Assemblies
-- Must be run against master database
-- ============================================================================
USE master;
GO

ALTER DATABASE [$(DatabaseName)] SET TRUSTWORTHY ON;
GO

PRINT 'TRUSTWORTHY enabled for $(DatabaseName)';

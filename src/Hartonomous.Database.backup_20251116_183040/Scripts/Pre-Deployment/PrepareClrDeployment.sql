/*
 Pre-Deployment Script for CLR Assembly Preparation
 This script ensures the database environment is ready for UNSAFE CLR assemblies
*/

PRINT 'Preparing database for CLR assembly deployment...'

-- Enable CLR at server level (if not already enabled)
DECLARE @clrEnabled INT
EXEC sp_configure 'show advanced options', 1
RECONFIGURE

-- Check current CLR enabled status
DECLARE @configTable TABLE (name NVARCHAR(128), minimum INT, maximum INT, config_value INT, run_value INT)
INSERT INTO @configTable
EXEC sp_configure 'clr enabled'

SELECT @clrEnabled = config_value FROM @configTable

IF @clrEnabled = 0
BEGIN
    PRINT 'Enabling CLR execution...'
    EXEC sp_configure 'clr enabled', 1
    RECONFIGURE
END

-- Set TRUSTWORTHY ON (required for UNSAFE assemblies without signed login)
IF DATABASEPROPERTYEX(DB_NAME(), 'IsTrustworthy') = 0
BEGIN
    PRINT 'Setting database TRUSTWORTHY property...'
    DECLARE @sql NVARCHAR(MAX) = N'ALTER DATABASE [' + DB_NAME() + N'] SET TRUSTWORTHY ON'
    EXEC sp_executesql @sql
END

-- Drop existing CLR functions/procedures that reference the old assembly
PRINT 'Dropping existing CLR objects...'

-- Dynamic script to drop all CLR functions, procedures, and triggers that reference Hartonomous.Clr assembly
DECLARE @sql NVARCHAR(MAX) = N''

-- Drop all CLR functions
SELECT @sql = @sql + N'DROP FUNCTION ' + QUOTENAME(SCHEMA_NAME(o.schema_id)) + N'.' + QUOTENAME(o.name) + N';' + CHAR(13) + CHAR(10)
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
WHERE o.type IN ('FN', 'FS', 'FT', 'IF', 'TF') -- All function types
  AND a.name IN ('Hartonomous.Clr', 'SqlClrFunctions')

-- Drop all CLR procedures
SELECT @sql = @sql + N'DROP PROCEDURE ' + QUOTENAME(SCHEMA_NAME(o.schema_id)) + N'.' + QUOTENAME(o.name) + N';' + CHAR(13) + CHAR(10)
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
WHERE o.type = 'PC' -- CLR stored procedure
  AND a.name IN ('Hartonomous.Clr', 'SqlClrFunctions')

-- Drop all CLR triggers
SELECT @sql = @sql + N'DROP TRIGGER ' + QUOTENAME(SCHEMA_NAME(o.schema_id)) + N'.' + QUOTENAME(o.name) + N';' + CHAR(13) + CHAR(10)
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
WHERE o.type = 'TA' -- CLR trigger
  AND a.name IN ('Hartonomous.Clr', 'SqlClrFunctions')

-- Execute dynamic drop statements
IF LEN(@sql) > 0
BEGIN
    PRINT 'Dropping CLR objects:'
    PRINT @sql
    EXEC sp_executesql @sql
END
ELSE
BEGIN
    PRINT 'No CLR objects found to drop'
END

-- Drop the old assembly if it exists
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'Hartonomous.Clr')
BEGIN
    PRINT 'Dropping existing Hartonomous.Clr assembly...'
    DROP ASSEMBLY [Hartonomous.Clr]
END

IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'SqlClrFunctions')
BEGIN
    PRINT 'Dropping obsolete SqlClrFunctions assembly...'
    DROP ASSEMBLY [SqlClrFunctions]
END

-- Note: Dependency assemblies (Microsoft.SqlServer.Types, MathNet.Numerics, Newtonsoft.Json)
-- are not dropped here as they may have dependencies and are managed by DACPAC deployment

PRINT 'CLR preparation complete.'
GO

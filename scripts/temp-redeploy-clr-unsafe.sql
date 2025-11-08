-- Temporary script to redeploy CLR assembly as UNSAFE
USE [Hartonomous];
GO

PRINT 'Dropping CLR functions...';
DECLARE @sql NVARCHAR(MAX) = '';

-- Drop CLR functions
SELECT @sql += 'DROP FUNCTION ' + QUOTENAME(SCHEMA_NAME(o.schema_id)) + '.' + QUOTENAME(o.name) + ';' + CHAR(13) + CHAR(10)
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
WHERE a.name = 'SqlClrFunctions' AND o.type IN ('FN', 'FS', 'FT', 'IF', 'TF');

EXEC sp_executesql @sql;
PRINT 'CLR functions dropped.';
GO

PRINT 'Dropping CLR aggregates...';
DECLARE @sql NVARCHAR(MAX) = '';

-- Drop CLR aggregates
SELECT @sql += 'DROP AGGREGATE ' + QUOTENAME(SCHEMA_NAME(o.schema_id)) + '.' + QUOTENAME(o.name) + ';' + CHAR(13) + CHAR(10)
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
WHERE a.name = 'SqlClrFunctions' AND o.type = 'AF';

EXEC sp_executesql @sql;
PRINT 'CLR aggregates dropped.';
GO

PRINT 'Dropping CLR types...';
DECLARE @sql NVARCHAR(MAX) = '';

-- Drop CLR types
SELECT @sql += 'DROP TYPE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ';' + CHAR(13) + CHAR(10)
FROM sys.types
WHERE is_assembly_type = 1
  AND assembly_id = (SELECT assembly_id FROM sys.assemblies WHERE name = 'SqlClrFunctions');

EXEC sp_executesql @sql;
PRINT 'CLR types dropped.';
GO

-- Drop assembly
PRINT 'Dropping assembly SqlClrFunctions...';
DROP ASSEMBLY [SqlClrFunctions];
PRINT 'Assembly dropped.';
GO

-- Now create the assembly as UNSAFE (this will be done separately with the DLL bytes)
PRINT 'Assembly ready for UNSAFE redeployment.';
PRINT 'Run the PowerShell deployment script or manually create with PERMISSION_SET = UNSAFE';
GO

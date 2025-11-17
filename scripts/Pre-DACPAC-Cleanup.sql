-- This script ensures idempotency by dropping all CLR-dependent objects
-- before the DACPAC deployment. This prevents "object already exists" errors
-- and handles dependency chains correctly.

USE [$(DatabaseName)];
GO

-- Drop all functions that depend on the main CLR assembly
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += 'DROP FUNCTION ' + QUOTENAME(s.name) + '.' + QUOTENAME(o.name) + ';' + CHAR(13)
FROM sys.assembly_modules am
JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
JOIN sys.objects o ON am.object_id = o.object_id
JOIN sys.schemas s ON o.schema_id = s.schema_id
WHERE a.name = 'Hartonomous.Clr';
EXEC sp_executesql @sql;
PRINT 'Dropped all CLR-dependent functions.';
GO

-- Drop all aggregates that depend on the main CLR assembly
DECLARE @sql_agg NVARCHAR(MAX) = N'';
SELECT @sql_agg += 'DROP AGGREGATE ' + QUOTENAME(s.name) + '.' + QUOTENAME(o.name) + ';' + CHAR(13)
FROM sys.assembly_modules am
JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
JOIN sys.objects o ON am.object_id = o.object_id
JOIN sys.schemas s ON o.schema_id = s.schema_id
WHERE a.name = 'Hartonomous.Clr' AND o.type = 'AF';
EXEC sp_executesql @sql_agg;
PRINT 'Dropped all CLR-dependent aggregates.';
GO

-- Drop all types that depend on the main CLR assembly
DECLARE @sql_types NVARCHAR(MAX) = N'';
SELECT @sql_types += 'DROP TYPE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ';' + CHAR(13)
FROM sys.assembly_types at
JOIN sys.assemblies a ON at.assembly_id = a.assembly_id
JOIN sys.types t ON at.user_type_id = t.user_type_id
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE a.name = 'Hartonomous.Clr';
EXEC sp_executesql @sql_types;
PRINT 'Dropped all CLR-dependent types.';
GO

-- Finally, drop the main CLR assembly itself
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'Hartonomous.Clr')
BEGIN
    DROP ASSEMBLY [Hartonomous.Clr];
    PRINT 'Dropped Hartonomous.Clr assembly.';
END
GO

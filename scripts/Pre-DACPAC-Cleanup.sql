-- This script ensures idempotency by dropping all CLR-dependent objects
-- before the DACPAC deployment. This prevents "object already exists" errors
-- and handles dependency chains correctly by dropping them in the reverse order of creation.

USE [$(DatabaseName)];
GO

-- Drop T-SQL objects that depend on other T-SQL objects, which in turn depend on CLR User-Defined Types
IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'ProvenanceValidationResults' AND type = 'U')
BEGIN
    DROP TABLE [dbo].[ProvenanceValidationResults];
    PRINT 'Dropped dependent table [dbo].[ProvenanceValidationResults].';
END
GO

-- Drop T-SQL objects that depend on CLR User-Defined Types
IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'OperationProvenance' AND type = 'U')
BEGIN
    DROP TABLE [dbo].[OperationProvenance];
    PRINT 'Dropped dependent table [dbo].[OperationProvenance].';
END
GO

-- Drop all CLR-mappable objects (Procedures, Functions, Aggregates)
DECLARE @sql_objects NVARCHAR(MAX) = N'';
SELECT @sql_objects += 
    'DROP ' +
    CASE o.type
        WHEN 'AF' THEN 'AGGREGATE'
        WHEN 'PC' THEN 'PROCEDURE'
        WHEN 'FS' THEN 'FUNCTION'
        WHEN 'FT' THEN 'FUNCTION'
        ELSE 'FUNCTION' -- Default for other function types like IF, TF
    END + ' ' + QUOTENAME(s.name) + '.' + QUOTENAME(o.name) + ';' + CHAR(13)
FROM sys.assembly_modules am
JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
JOIN sys.objects o ON am.object_id = o.object_id
JOIN sys.schemas s ON o.schema_id = s.schema_id
WHERE a.name = 'Hartonomous.Clr';

EXEC sp_executesql @sql_objects;
PRINT 'Dropped all CLR-dependent procedures, functions, and aggregates.';
GO

-- Drop all CLR UDTs (User-Defined Types)
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
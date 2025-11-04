-- Deploy SqlClrFunctions with UNSAFE permission (improved version)
-- Handles type dependencies correctly

USE Hartonomous;
GO

-- Drop all dependent objects in correct order
PRINT 'Step 1: Dropping CLR functions...';

-- Drop FileSystem functions (may not exist yet)
IF OBJECT_ID('dbo.clr_WriteFileBytes', 'FS') IS NOT NULL
BEGIN
    DROP FUNCTION dbo.clr_WriteFileBytes;
    PRINT '  - Dropped clr_WriteFileBytes';
END

IF OBJECT_ID('dbo.clr_ReadFileBytes', 'FS') IS NOT NULL
BEGIN
    DROP FUNCTION dbo.clr_ReadFileBytes;
    PRINT '  - Dropped clr_ReadFileBytes';
END

IF OBJECT_ID('dbo.clr_ExecuteShellCommand', 'FT') IS NOT NULL
BEGIN
    DROP FUNCTION dbo.clr_ExecuteShellCommand;
    PRINT '  - Dropped clr_ExecuteShellCommand';
END

IF OBJECT_ID('dbo.clr_ListDirectory', 'FT') IS NOT NULL
BEGIN
    DROP FUNCTION dbo.clr_ListDirectory;
    PRINT '  - Dropped clr_ListDirectory';
END

-- Drop all other functions that reference the assembly
DECLARE @dropFunctions NVARCHAR(MAX) = '';
SELECT @dropFunctions = @dropFunctions + 'DROP FUNCTION ' + QUOTENAME(SCHEMA_NAME(o.schema_id)) + '.' + QUOTENAME(o.name) + '; '
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
WHERE a.name = 'SqlClrFunctions' AND o.type IN ('FS', 'FT');

IF LEN(@dropFunctions) > 0
BEGIN
    PRINT 'Dropping functions: ' + @dropFunctions;
    EXEC sp_executesql @dropFunctions;
END

-- Drop aggregates
PRINT 'Step 2: Dropping CLR aggregates...';
DECLARE @dropAggregates NVARCHAR(MAX) = '';
SELECT @dropAggregates = @dropAggregates + 'DROP AGGREGATE ' + QUOTENAME(SCHEMA_NAME(o.schema_id)) + '.' + QUOTENAME(o.name) + '; '
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
WHERE a.name = 'SqlClrFunctions' AND o.type = 'AF';

IF LEN(@dropAggregates) > 0
BEGIN
    PRINT 'Dropping aggregates: ' + @dropAggregates;
    EXEC sp_executesql @dropAggregates;
END

-- Drop types (these block assembly drops)
PRINT 'Step 3: Dropping CLR types...';
IF TYPE_ID('provenance.AtomicStream') IS NOT NULL
BEGIN
    DROP TYPE provenance.AtomicStream;
    PRINT '  - Dropped provenance.AtomicStream';
END

IF TYPE_ID('provenance.ComponentStream') IS NOT NULL
BEGIN
    DROP TYPE provenance.ComponentStream;
    PRINT '  - Dropped provenance.ComponentStream';
END

-- Now drop the assembly
PRINT 'Step 4: Dropping SqlClrFunctions assembly...';
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'SqlClrFunctions')
BEGIN
    DROP ASSEMBLY SqlClrFunctions;
    PRINT '  - SqlClrFunctions assembly dropped';
END
GO

-- Create assembly with UNSAFE permission
PRINT 'Step 5: Creating SqlClrFunctions assembly with UNSAFE permission...';
DECLARE @assemblyPath NVARCHAR(500) = 'd:\Repositories\Hartonomous\src\SqlClr\bin\Release\SqlClrFunctions.dll';

CREATE ASSEMBLY SqlClrFunctions
FROM 'd:\Repositories\Hartonomous\src\SqlClr\bin\Release\SqlClrFunctions.dll'
WITH PERMISSION_SET = UNSAFE;

PRINT '  - SqlClrFunctions assembly created with UNSAFE permission';
GO

-- Recreate types
PRINT 'Step 6: Recreating CLR types...';
CREATE TYPE provenance.AtomicStream
EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AtomicStream];
PRINT '  - Created provenance.AtomicStream';

CREATE TYPE provenance.ComponentStream
EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ComponentStream];
PRINT '  - Created provenance.ComponentStream';
GO

-- Verify assembly
PRINT 'Step 7: Verifying assembly deployment...';
SELECT 
    a.name AS AssemblyName,
    a.permission_set_desc AS PermissionSet,
    a.create_date,
    a.is_visible
FROM sys.assemblies a
WHERE a.name = 'SqlClrFunctions';
GO

PRINT '';
PRINT 'SUCCESS: CLR assembly deployment complete with UNSAFE permission!';
PRINT 'Next step: Execute sql\procedures\Autonomy.FileSystemBindings.sql';

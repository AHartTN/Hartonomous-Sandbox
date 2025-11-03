-- Registers the SqlClrFunctions assembly and AtomicStream user-defined type.
-- Requires SQLCMD mode so the $(SqlClrAssemblyPath) variable can be supplied at execution time.
-- Example: sqlcmd -S . -d Hartonomous -i provenance.AtomicStream.sql -v SqlClrAssemblyPath="D:\\deploy\\SqlClrFunctions.dll"

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'provenance')
BEGIN
    EXEC('CREATE SCHEMA provenance AUTHORIZATION dbo');
END;
GO

IF EXISTS (SELECT 1 FROM sys.types WHERE name = 'AtomicStream' AND schema_id = SCHEMA_ID('provenance'))
    DROP TYPE provenance.AtomicStream;
GO

IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'SqlClrFunctions')
BEGIN
    DROP ASSEMBLY SqlClrFunctions WITH NO DEPENDENTS;
END;
GO

DECLARE @assemblyPath NVARCHAR(4000) = '$(SqlClrAssemblyPath)';
DECLARE @createAssembly NVARCHAR(MAX) = N'CREATE ASSEMBLY SqlClrFunctions FROM ''' + @assemblyPath + N''' WITH PERMISSION_SET = SAFE;';
EXEC (@createAssembly);
GO

CREATE TYPE provenance.AtomicStream
EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AtomicStream];
GO

PRINT 'SqlClrFunctions assembly and provenance.AtomicStream type created.';
PRINT 'Re-apply sql\\procedures\\Common.ClrBindings.sql after running this script to restore CLR helper functions.';
GO

-- =============================================
-- Deploy Main Hartonomous.Clr Assembly with UNSAFE Permission Set
-- =============================================
-- This script deploys the main CLR assembly compiled from the database project.
-- It must be deployed AFTER dependencies are registered and AFTER the assembly is trusted.
-- =============================================

USE [Hartonomous];
GO

SET NOCOUNT ON;

PRINT '=== Deploying Main Hartonomous.Clr Assembly ===';
PRINT '';

DECLARE @AssemblyName NVARCHAR(128) = N'Hartonomous.Clr';
DECLARE @DllPath NVARCHAR(512) = N'D:\Repositories\Hartonomous\src\Hartonomous.Database\bin\Output\Hartonomous.Database.dll';
DECLARE @ExpectedVersion NVARCHAR(50) = N'1.0.0.0';

-- Check if file exists
IF NOT EXISTS (
    SELECT 1 FROM sys.dm_os_file_exists(@DllPath) WHERE file_exists = 1
)
BEGIN
    RAISERROR('Assembly DLL not found at: %s', 16, 1, @DllPath);
    RETURN;
END

-- Get current version if assembly exists
DECLARE @CurrentVersion NVARCHAR(50);
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = @AssemblyName)
BEGIN
    SELECT @CurrentVersion = 
        CONVERT(NVARCHAR(100), ASSEMBLYPROPERTY(name, 'VersionMajor')) + '.' +
        CONVERT(NVARCHAR(100), ASSEMBLYPROPERTY(name, 'VersionMinor')) + '.' +
        CONVERT(NVARCHAR(100), ASSEMBLYPROPERTY(name, 'VersionBuild')) + '.' +
        CONVERT(NVARCHAR(100), ASSEMBLYPROPERTY(name, 'VersionRevision'))
    FROM sys.assemblies
    WHERE name = @AssemblyName;
    
    IF @CurrentVersion = @ExpectedVersion
    BEGIN
        PRINT '  ○ ' + @AssemblyName + ' ' + @CurrentVersion + ' already deployed';
        PRINT '';
        PRINT '=== Main Assembly Deployment Complete ===';
        RETURN;
    END
    
    PRINT '  ! Version mismatch: Current = ' + @CurrentVersion + ', Expected = ' + @ExpectedVersion;
    PRINT '  → Dropping existing assembly...';
    
    -- Drop assembly (will cascade drop all dependent objects - functions, procedures, UDTs, aggregates)
    DECLARE @DropSql NVARCHAR(MAX) = N'DROP ASSEMBLY [' + @AssemblyName + N'];';
    EXEC sp_executesql @DropSql;
    
    PRINT '  ✓ Existing assembly dropped';
END

-- Create assembly with UNSAFE permission set
PRINT '  → Creating assembly ' + @AssemblyName + ' ' + @ExpectedVersion + ' with PERMISSION_SET = UNSAFE...';

DECLARE @CreateSql NVARCHAR(MAX) = 
    N'CREATE ASSEMBLY [' + @AssemblyName + N'] ' +
    N'FROM N''' + @DllPath + N''' ' +
    N'WITH PERMISSION_SET = UNSAFE;';

BEGIN TRY
    EXEC sp_executesql @CreateSql;
    PRINT '  ✓ ' + @AssemblyName + ' ' + @ExpectedVersion + ' deployed successfully';
END TRY
BEGIN CATCH
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    PRINT '  ✗ Failed to create assembly: ' + @ErrorMessage;
    RAISERROR('Assembly deployment failed: %s', 16, 1, @ErrorMessage);
    RETURN;
END CATCH

PRINT '';
PRINT '=== Main Assembly Deployment Complete ===';
PRINT '';
PRINT 'NOTE: CLR objects (functions, procedures, UDTs, aggregates) will be deployed by DACPAC.';
PRINT '      Run DACPAC deployment next to create the CLR objects that reference this assembly.';
GO

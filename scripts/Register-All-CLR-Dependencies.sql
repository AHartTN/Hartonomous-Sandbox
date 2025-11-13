-- ===============================================================================
-- Register ALL CLR dependencies before DACPAC deployment
-- ===============================================================================
-- Run this AFTER Setup-CLR-Security.sql and Trust-All-Dependencies.sql
-- Run this BEFORE SqlPackage deployment
-- IDEMPOTENT: Checks versions and drops/recreates if version mismatch
--
-- STRATEGY:
--   GAC Assemblies: Register from Global Assembly Cache (avoid signature conflicts)
--   Non-GAC: Register from dependencies folder (.NET Framework 4.8.1 versions)
-- ===============================================================================

USE [Hartonomous];
GO

-- Helper: Register assembly from dependencies folder with version check
CREATE OR ALTER PROCEDURE #RegisterDependency
    @AssemblyName NVARCHAR(128),
    @FileName NVARCHAR(256),
    @ExpectedVersion NVARCHAR(50)
AS
BEGIN
    DECLARE @CurrentVersion NVARCHAR(50);
    DECLARE @Sql NVARCHAR(MAX);
    DECLARE @DepsPath NVARCHAR(500) = N'D:\Repositories\Hartonomous\dependencies\';
    
    SELECT @CurrentVersion = CAST(ASSEMBLYPROPERTY(name, 'VersionMajor') AS NVARCHAR) + '.' + 
                             CAST(ASSEMBLYPROPERTY(name, 'VersionMinor') AS NVARCHAR) + '.' + 
                             CAST(ASSEMBLYPROPERTY(name, 'VersionBuild') AS NVARCHAR) + '.' + 
                             CAST(ASSEMBLYPROPERTY(name, 'VersionRevision') AS NVARCHAR)
    FROM sys.assemblies WHERE name = @AssemblyName;
    
    IF @CurrentVersion IS NOT NULL AND @CurrentVersion <> @ExpectedVersion
    BEGIN
        PRINT '  ⚠ ' + @AssemblyName + ' version mismatch (found ' + @CurrentVersion + ', expected ' + @ExpectedVersion + ')';
        PRINT '    Dropping and recreating...';
        SET @Sql = N'DROP ASSEMBLY [' + @AssemblyName + N'];';
        EXEC sp_executesql @Sql;
        SET @CurrentVersion = NULL;
    END
    
    IF @CurrentVersion IS NULL
    BEGIN
        PRINT '  Registering ' + @AssemblyName + ' ' + @ExpectedVersion + ' [DEPS]...';
        SET @Sql = N'CREATE ASSEMBLY [' + @AssemblyName + N'] FROM N''' + @DepsPath + @FileName + N''' WITH PERMISSION_SET = UNSAFE;';
        EXEC sp_executesql @Sql;
        PRINT '  ✓ ' + @AssemblyName + ' registered';
    END
    ELSE
        PRINT '  ○ ' + @AssemblyName + ' ' + @CurrentVersion + ' already registered [DEPS]';
END
GO

-- Helper: Register assembly from GAC with version check
CREATE OR ALTER PROCEDURE #RegisterFromGAC
    @AssemblyName NVARCHAR(128),
    @GACPath NVARCHAR(500),
    @ExpectedVersion NVARCHAR(50)
AS
BEGIN
    DECLARE @CurrentVersion NVARCHAR(50);
    DECLARE @Sql NVARCHAR(MAX);
    
    SELECT @CurrentVersion = CAST(ASSEMBLYPROPERTY(name, 'VersionMajor') AS NVARCHAR) + '.' + 
                             CAST(ASSEMBLYPROPERTY(name, 'VersionMinor') AS NVARCHAR) + '.' + 
                             CAST(ASSEMBLYPROPERTY(name, 'VersionBuild') AS NVARCHAR) + '.' + 
                             CAST(ASSEMBLYPROPERTY(name, 'VersionRevision') AS NVARCHAR)
    FROM sys.assemblies WHERE name = @AssemblyName;
    
    IF @CurrentVersion IS NOT NULL AND @CurrentVersion <> @ExpectedVersion
    BEGIN
        PRINT '  ⚠ ' + @AssemblyName + ' version mismatch (found ' + @CurrentVersion + ', expected ' + @ExpectedVersion + ')';
        PRINT '    Dropping and recreating...';
        SET @Sql = N'DROP ASSEMBLY [' + @AssemblyName + N'];';
        EXEC sp_executesql @Sql;
        SET @CurrentVersion = NULL;
    END
    
    IF @CurrentVersion IS NULL
    BEGIN
        PRINT '  Registering ' + @AssemblyName + ' ' + @ExpectedVersion + ' [GAC]...';
        SET @Sql = N'CREATE ASSEMBLY [' + @AssemblyName + N'] FROM N''' + @GACPath + N''' WITH PERMISSION_SET = UNSAFE;';
        EXEC sp_executesql @Sql;
        PRINT '  ✓ ' + @AssemblyName + ' registered';
    END
    ELSE
        PRINT '  ○ ' + @AssemblyName + ' ' + @CurrentVersion + ' already registered [GAC]';
END
GO


PRINT '=== Registering CLR Dependencies (Idempotent) ===';
PRINT '';

-- ========================================
-- NON-GAC DEPENDENCIES (from dependencies folder)
-- These are NuGet packages for .NET Framework 4.8.1
-- Register base dependencies first, then those with dependencies
-- ========================================
PRINT '--- Non-GAC Dependencies (from dependencies folder) ---';

-- Base dependencies (no CLR dependencies)
EXEC #RegisterDependency 'System.Runtime.CompilerServices.Unsafe', 'System.Runtime.CompilerServices.Unsafe.dll', '4.0.4.1';
EXEC #RegisterDependency 'System.Buffers', 'System.Buffers.dll', '4.0.3.0';

-- System.Numerics.Vectors: GAC has 4.0.0.0 but we need 4.1.4.0 for MathNet.Numerics
EXEC #RegisterDependency 'System.Numerics.Vectors', 'System.Numerics.Vectors.dll', '4.1.4.0';

-- Dependencies that require Buffers/Unsafe
EXEC #RegisterDependency 'System.Memory', 'System.Memory.dll', '4.0.1.1';
EXEC #RegisterDependency 'System.Collections.Immutable', 'System.Collections.Immutable.dll', '1.2.5.0';
EXEC #RegisterDependency 'System.Reflection.Metadata', 'System.Reflection.Metadata.dll', '1.4.5.0';

PRINT '';

-- Note: MathNet.Numerics will be registered AFTER GAC dependencies (requires System.ValueTuple)
PRINT '--- Deferred: MathNet.Numerics (requires System.ValueTuple from GAC) ---';
PRINT '';

-- ========================================
-- GAC DEPENDENCIES (from Global Assembly Cache)
-- These are .NET Framework system assemblies
-- Register from GAC to avoid signature conflicts
-- CRITICAL: Order matters! Dependencies must be registered before dependents:
--   1. SMDiagnostics (no dependencies)
--   2. System.ServiceModel.Internals (depends on SMDiagnostics)
--   3. System.Runtime.Serialization (depends on SMDiagnostics, System.ServiceModel.Internals)
--   4. Newtonsoft.Json (depends on System.Runtime.Serialization)
--   5. System.ValueTuple (for MathNet.Numerics)
--   6. System.Drawing (independent)
-- ========================================
PRINT '--- GAC Dependencies (from Global Assembly Cache) ---';

-- Step 1: SMDiagnostics (base dependency)
EXEC #RegisterFromGAC 'SMDiagnostics', 
    'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\SMDiagnostics\v4.0_4.0.0.0__b77a5c561934e089\SMDiagnostics.dll',
    '4.0.0.0';

-- Step 2: System.ServiceModel.Internals (depends on SMDiagnostics)
EXEC #RegisterFromGAC 'System.ServiceModel.Internals', 
    'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.ServiceModel.Internals\v4.0_4.0.0.0__31bf3856ad364e35\System.ServiceModel.Internals.dll',
    '4.0.0.0';

-- Step 3: System.Runtime.Serialization (depends on SMDiagnostics, System.ServiceModel.Internals)
EXEC #RegisterFromGAC 'System.Runtime.Serialization', 
    'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Runtime.Serialization\v4.0_4.0.0.0__b77a5c561934e089\System.Runtime.Serialization.dll',
    '4.0.0.0';

-- Step 4: Newtonsoft.Json (depends on System.Runtime.Serialization)
EXEC #RegisterFromGAC 'Newtonsoft.Json', 
    'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\Newtonsoft.Json\v4.0_13.0.0.0__30ad4fe6b2a6aeed\Newtonsoft.Json.dll',
    '13.0.0.0';

-- Step 5: System.ValueTuple (required by MathNet.Numerics)
EXEC #RegisterFromGAC 'System.ValueTuple', 
    'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.ValueTuple\v4.0_4.0.0.0__cc7b13ffcd2ddd51\System.ValueTuple.dll',
    '4.0.0.0';

-- Step 6: System.Drawing (independent)
EXEC #RegisterFromGAC 'System.Drawing', 
    'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Drawing\v4.0_4.0.0.0__b03f5f7f11d50a3a\System.Drawing.dll',
    '4.0.0.0';

-- Note: Microsoft.SqlServer.Types is typically installed by SQL Server (version 17.0.0.0 on this system)
-- If not present, it will be registered during DACPAC deployment
PRINT '';

-- ========================================
-- FINAL: MathNet.Numerics (requires System.ValueTuple)
-- Must be registered AFTER GAC dependencies are loaded
-- ========================================
PRINT '--- Final: MathNet.Numerics (depends on System.ValueTuple) ---';
EXEC #RegisterDependency 'MathNet.Numerics', 'MathNet.Numerics.dll', '5.0.0.0';
PRINT '';
GO

DROP PROCEDURE #RegisterDependency;
DROP PROCEDURE #RegisterFromGAC;
GO

PRINT '=== All CLR Dependencies Registered Successfully ===';



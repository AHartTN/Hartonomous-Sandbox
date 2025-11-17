/*
================================================================================
CLR Assembly Registration - SqlClrFunctions (PRE-DEPLOYMENT)
================================================================================
Purpose: Registers the SqlClrFunctions.dll assembly BEFORE DACPAC deployment
         to enable CLR functions, aggregates, and procedures.

WHY PRE-DEPLOYMENT:
- DACPAC functions reference [SqlClrFunctions] assembly in their definitions
- Assembly MUST exist BEFORE functions are created
- CREATE ASSEMBLY must happen first, CREATE FUNCTION second (MS Docs pattern)

Requirements:
- SQL Server CLR integration enabled: sp_configure 'clr enabled', 1
- Database trustworthy OR assembly signed with asymmetric key
- UNSAFE permission required for:
  - File I/O operations
  - Audio signal processing (RMS, Peak, FFT)
  - Vector operations (normalization, cosine similarity)
  - Statistical computations (Z-score, churn prediction)

SQLCMD Variables:
- $(SqlClrKeyPath): Full path to SqlClrKey.snk strong name key file
  Default: D:\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\CLR\SqlClrKey.snk
  Override: sqlpackage /v:SqlClrKeyPath="C:\Custom\Path\SqlClrKey.snk"

- $(MathNetPath): Full path to MathNet.Numerics.dll assembly
  Default: D:\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\CLR\Dependencies\MathNet.Numerics.dll
  Override: sqlpackage /v:MathNetPath="C:\Custom\Path\MathNet.Numerics.dll"

Security Note:
- UNSAFE assemblies require sysadmin permissions or certificate/asymmetric key signing
- Review SqlClrFunctions.dll code before deployment to production
================================================================================
*/

SET NOCOUNT ON;
PRINT '=======================================================';
PRINT 'CLR ASSEMBLY REGISTRATION - SqlClrFunctions';
PRINT '=======================================================';
PRINT '';

-- Check if CLR integration is enabled
DECLARE @clr_enabled INT;
SELECT @clr_enabled = CAST(value AS INT) 
FROM sys.configurations 
WHERE name = 'clr enabled';

IF @clr_enabled = 0
BEGIN
    PRINT '⚠ CLR integration is DISABLED. Enabling CLR...';
    EXEC sp_configure 'clr enabled', 1;
    RECONFIGURE;
    PRINT '✓ CLR integration enabled';
END
ELSE
    PRINT '○ CLR integration already enabled';

PRINT '';
GO

-- ================================================================================
-- CLR STRICT SECURITY PREREQUISITE CHECK
-- ================================================================================
-- IMPORTANT: Before deploying this DACPAC, run Scripts\Setup-CLR-Security.sql
-- 
-- That script creates (incompatible with DACFx):
--   1. Asymmetric key from SqlClrKey.snk
--   2. Login from asymmetric key  
--   3. UNSAFE ASSEMBLY permission grant
--
-- Execute: sqlcmd -S localhost -d master -E -i Scripts\Setup-CLR-Security.sql
-- ================================================================================

USE master;
GO

PRINT '  Verifying CLR security prerequisites...';

-- Verify asymmetric key exists
IF NOT EXISTS (SELECT 1 FROM sys.asymmetric_keys WHERE name = 'SqlClrAsymmetricKey')
BEGIN
    RAISERROR('ERROR: Asymmetric key SqlClrAsymmetricKey not found. Run Scripts\Setup-CLR-Security.sql first.', 16, 1);
END

-- Verify login exists
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'SqlClrLogin')
BEGIN
    RAISERROR('ERROR: Login SqlClrLogin not found. Run Scripts\Setup-CLR-Security.sql first.', 16, 1);
END

-- Verify UNSAFE ASSEMBLY permission
IF NOT EXISTS (
    SELECT 1 
    FROM sys.server_permissions sp
    INNER JOIN sys.server_principals sl ON sp.grantee_principal_id = sl.principal_id
    WHERE sl.name = 'SqlClrLogin' 
    AND sp.permission_name = 'UNSAFE ASSEMBLY'
    AND sp.state = 'G'
)
BEGIN
    RAISERROR('ERROR: SqlClrLogin missing UNSAFE ASSEMBLY permission. Run Scripts\Setup-CLR-Security.sql first.', 16, 1);
END

PRINT '  ✓ CLR security prerequisites verified';

GO

-- Switch back to target database
PRINT '';
GO

-- ================================================================================
-- SWITCH TO TARGET DATABASE FOR ASSEMBLY REGISTRATION
-- ================================================================================

USE [Hartonomous];
GO

-- Create database user for SqlClrLogin (required for UNSAFE assemblies)
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'SqlClrLogin')
BEGIN
    PRINT '  Creating database user for SqlClrLogin...';
    CREATE USER [SqlClrLogin] FROM LOGIN [SqlClrLogin];
    PRINT '  ✓ SqlClrLogin user created';
END
ELSE
    PRINT '  ○ SqlClrLogin user already exists';
GO

-- Drop existing assembly if it exists (must drop AFTER dropping dependent objects in DACPAC)
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'SqlClrFunctions')
BEGIN
    PRINT '  Dropping existing SqlClrFunctions assembly...';
    DROP ASSEMBLY [SqlClrFunctions];
    PRINT '  ✓ Existing assembly dropped';
END
GO

PRINT '';
PRINT '  Registering dependency assemblies...';
GO

-- STRATEGY: Let DACPAC handle CLR object recreation through its deployment plan
-- We only need to drop/recreate ASSEMBLIES with signature mismatches
-- DACPAC will automatically: DROP old CLR objects → DROP old assembly → CREATE new assembly → CREATE new CLR objects

-- Drop existing Newtonsoft.Json assembly ONLY if it has wrong hash
-- Check assembly hash to avoid unnecessary drops during idempotent deployments
IF EXISTS (
    SELECT 1 
    FROM sys.assemblies a
    JOIN sys.assembly_files af ON a.assembly_id = af.assembly_id
    WHERE a.name = 'Newtonsoft.Json'
    AND CONVERT(VARCHAR(MAX), HASHBYTES('SHA2_512', af.content), 2) <> '72D9C7F3587F82ACAA76F3DEDC3D4E470F3BFDC9153D2A5B58EFE9AFC8A16975590EFD85204C90F63CF44A5712E9D7F1DBEF63D5DC3B7BA852639B31117EFBEF'
)
BEGIN
    PRINT '  Detected Newtonsoft.Json with wrong hash - DACPAC will replace during deployment';
    -- DACPAC handles the drop automatically as part of deployment plan
END
ELSE IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'Newtonsoft.Json')
BEGIN
    PRINT '  Newtonsoft.Json not yet registered - will be created during dependency registration';
END
ELSE
BEGIN
    PRINT '  ○ Newtonsoft.Json (GAC version) already correct';
END
GO

/*
================================================================================
DEPENDENCY ASSEMBLY REGISTRATION STRATEGY
================================================================================
GAC ASSEMBLIES (Auto-loaded by SQL Server - DO NOT manually deploy):
  - Newtonsoft.Json v13.0.0.0 (in GAC, SQL Server loads automatically)
  - System.Numerics.Vectors v4.0.0.0 (in GAC, SQL Server loads automatically)
  - System.ValueTuple v4.0.0.0 (in GAC, SQL Server loads automatically)

MANUAL DEPLOYMENT ASSEMBLIES (Must register explicitly):
  - All other dependencies listed below
  - ILGPU and ILGPU.Algorithms for GPU acceleration
  - MathNet.Numerics for advanced math operations
  - System.* polyfill assemblies not in GAC

WHY: Manually deploying GAC assemblies causes MVID signature mismatches.
SQL Server's CLR host automatically loads from GAC when assemblies are strong-named
and available in the Global Assembly Cache.
================================================================================
*/

-- ALL dependencies from $(DependenciesPath) - register dependencies BEFORE SqlClrFunctions
-- These are the EXACT versions required by SqlClrFunctions.dll dependency tree
-- Must run Scripts\Trust-GAC-Assemblies.sql FIRST to trust these assemblies

-- System.Runtime.CompilerServices.Unsafe (4.0.4.1)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Runtime.CompilerServices.Unsafe')
BEGIN
    PRINT '  Registering System.Runtime.CompilerServices.Unsafe...';
    CREATE ASSEMBLY [System.Runtime.CompilerServices.Unsafe]
    FROM '$(DependenciesPath)\System.Runtime.CompilerServices.Unsafe.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ System.Runtime.CompilerServices.Unsafe registered';
END
ELSE
    PRINT '  ○ System.Runtime.CompilerServices.Unsafe already registered';

GO

-- System.Buffers (4.0.3.0)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Buffers')
BEGIN
    PRINT '  Registering System.Buffers...';
    CREATE ASSEMBLY [System.Buffers]
    FROM '$(DependenciesPath)\System.Buffers.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ System.Buffers registered';
END
ELSE
    PRINT '  ○ System.Buffers already registered';

GO

-- System.Memory (4.0.1.1)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Memory')
BEGIN
    PRINT '  Registering System.Memory...';
    CREATE ASSEMBLY [System.Memory]
    FROM '$(DependenciesPath)\System.Memory.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ System.Memory registered';
END
ELSE
    PRINT '  ○ System.Memory already registered';

GO

-- System.Numerics.Vectors - SKIP (in GAC v4.0.0.0, auto-loaded by SQL Server)
-- Manually deploying causes MVID signature mismatch errors
PRINT '  ○ System.Numerics.Vectors (using GAC version)';

GO

-- System.Collections.Immutable (1.2.5.0)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Collections.Immutable')
BEGIN
    PRINT '  Registering System.Collections.Immutable...';
    CREATE ASSEMBLY [System.Collections.Immutable]
    FROM '$(DependenciesPath)\System.Collections.Immutable.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ System.Collections.Immutable registered';
END
ELSE
    PRINT '  ○ System.Collections.Immutable already registered';

GO

-- System.Reflection.Metadata (1.4.5.0)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Reflection.Metadata')
BEGIN
    PRINT '  Registering System.Reflection.Metadata...';
    CREATE ASSEMBLY [System.Reflection.Metadata]
    FROM '$(DependenciesPath)\System.Reflection.Metadata.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ System.Reflection.Metadata registered';
END
ELSE
    PRINT '  ○ System.Reflection.Metadata already registered';

GO

-- System.Drawing - GAC assembly, must be registered
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Drawing')
BEGIN
    PRINT '  Registering System.Drawing...';
    CREATE ASSEMBLY [System.Drawing]
    FROM 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Drawing.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ System.Drawing registered';
END
ELSE
    PRINT '  ○ System.Drawing (GAC assembly)';

GO

-- SMDiagnostics - GAC assembly, referenced but not deployed
PRINT '  ○ SMDiagnostics (GAC assembly)';

GO

-- System.ServiceModel.Internals - GAC assembly, referenced but not deployed
PRINT '  ○ System.ServiceModel.Internals (GAC assembly)';

GO

-- System.Runtime.Serialization - GAC assembly, referenced but not deployed
PRINT '  ○ System.Runtime.Serialization (GAC assembly)';

GO

-- MathNet.Numerics (5.0.0.0)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'MathNet.Numerics')
BEGIN
    PRINT '  Registering MathNet.Numerics...';
    CREATE ASSEMBLY [MathNet.Numerics]
    FROM '$(DependenciesPath)\MathNet.Numerics.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ MathNet.Numerics registered';
END
ELSE
    PRINT '  ○ MathNet.Numerics already registered';

GO

-- Microsoft.SqlServer.Types (16.0.0.0)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'Microsoft.SqlServer.Types')
BEGIN
    PRINT '  Registering Microsoft.SqlServer.Types...';
    CREATE ASSEMBLY [Microsoft.SqlServer.Types]
    FROM '$(DependenciesPath)\Microsoft.SqlServer.Types.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ Microsoft.SqlServer.Types registered';
END
ELSE
    PRINT '  ○ Microsoft.SqlServer.Types already registered';

GO

-- Newtonsoft.Json (13.0.0.0) - LOAD FROM GAC (v13 in GAC)
-- dependencies\Newtonsoft.Json.dll hash conflicts with GAC - use GAC version to avoid signature mismatch
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'Newtonsoft.Json')
BEGIN
    PRINT '  Registering Newtonsoft.Json (from GAC)...';
    CREATE ASSEMBLY [Newtonsoft.Json]
    FROM 'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\Newtonsoft.Json\v4.0_13.0.0.0__30ad4fe6b2a6aeed\Newtonsoft.Json.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ Newtonsoft.Json registered (GAC version)';
END
ELSE
    PRINT '  ○ Newtonsoft.Json already registered';

GO

PRINT '';
PRINT '=======================================================';
PRINT 'CLR DEPENDENCY REGISTRATION COMPLETE';
PRINT 'Main assembly (Hartonomous.Database) will be deployed by DACPAC';
PRINT '=======================================================';
GO

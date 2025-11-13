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

-- Drop existing assembly if it exists
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'SqlClrFunctions')
BEGIN
    PRINT '  Dropping existing SqlClrFunctions assembly...';
    DROP ASSEMBLY [SqlClrFunctions];
    PRINT '  ✓ Existing assembly dropped';
END
GO

-- Register the assembly with UNSAFE permissions
PRINT '  SqlClrFunctions assembly will be deployed from DACPAC';
PRINT '  Assembly is signed with SqlClrKey.snk and will use SqlClrLogin';
PRINT '';
GO

-- ALL dependencies from $(DependenciesPath) - NEVER use GAC versions
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

-- System.Numerics.Vectors (4.1.4.0) - CORRECT version from dependencies
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Numerics.Vectors')
BEGIN
    PRINT '  Registering System.Numerics.Vectors (4.1.4.0)...';
    CREATE ASSEMBLY [System.Numerics.Vectors]
    FROM '$(DependenciesPath)\System.Numerics.Vectors.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ System.Numerics.Vectors registered';
END
ELSE
    PRINT '  ○ System.Numerics.Vectors already registered';

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

-- System.Drawing (from dependencies, not GAC)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Drawing')
BEGIN
    PRINT '  Registering System.Drawing...';
    CREATE ASSEMBLY [System.Drawing]
    FROM '$(DependenciesPath)\System.Drawing.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ System.Drawing registered';
END
ELSE
    PRINT '  ○ System.Drawing already registered';

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

-- Newtonsoft.Json (13.0.0.0)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'Newtonsoft.Json')
BEGIN
    PRINT '  Registering Newtonsoft.Json...';
    CREATE ASSEMBLY [Newtonsoft.Json]
    FROM '$(DependenciesPath)\Newtonsoft.Json.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ Newtonsoft.Json registered';
END
ELSE
    PRINT '  ○ Newtonsoft.Json already registered';

PRINT '';
GO

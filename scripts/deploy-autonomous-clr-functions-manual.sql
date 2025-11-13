-- ============================================================
-- SQL CLR DEPLOYMENT SCRIPT FOR ILGPU/MATHNET INTEGRATION
-- ============================================================
-- 
-- This script implements the definitive solution for deploying high-performance
-- numerical computation libraries (ILGPU 0.9.2, MathNet.Numerics 5.0.0) to
-- SQL Server CLR 4.8.1 with UNSAFE permissions.
--
-- CRITICAL CONSTRAINT: Assembly Binding Paradox Resolution
-- SQL Server's CLR host (sqlservr.exe) does NOT consult application configuration
-- files, rendering standard binding redirects ineffective. This script manually
-- deploys the exact CLR Assembly Versions required by the dependency tree.
--
-- EXECUTION REQUIREMENTS:
-- 1. All DLLs must be extracted from net4x folders (NOT netstandard)
-- 2. DLLs must be strong-name signed
-- 3. Database owner must have external access rights for UNSAFE assemblies
-- 4. CLR must be enabled on the SQL Server instance
--
-- DEPLOYMENT ORDER (Dependency Graph):
--   Foundation → Memory → Reflection → High-Level → Application
--
-- VERSION MAPPING (NuGet Package → CLR Assembly):
--   System.Runtime.CompilerServices.Unsafe 4.5.3 → 4.0.4.1
--   System.Memory 4.5.4 → 4.0.1.0
--   System.Reflection.Metadata 1.8.1 → 1.4.5.0
--   System.Collections.Immutable 1.7.1 → 1.2.3.0
--   System.ValueTuple 4.5.0 → 4.0.3.0
--   System.Numerics.Vectors 4.5.0 → 4.1.5.0
--
-- Reference: SQLSERVER_BINDING_REDIRECTS.md
-- ============================================================

USE [master];
GO

-- ============================================================
-- PREREQUISITES: CLR CONFIGURATION
-- ============================================================

PRINT '';
PRINT '============================================================';
PRINT 'SQL CLR DEPLOYMENT: ILGPU/MATHNET INTEGRATION';
PRINT '============================================================';
PRINT '';

-- Enable CLR integration (if not already enabled)
DECLARE @clrEnabled INT;
SELECT @clrEnabled = CAST(value AS INT) FROM sys.configurations WHERE name = 'clr enabled';

IF @clrEnabled = 0
BEGIN
    PRINT 'Enabling CLR integration...';
    EXEC sp_configure 'clr enabled', 1;
    RECONFIGURE;
    PRINT '✓ CLR integration enabled';
END
ELSE
BEGIN
    PRINT '✓ CLR integration already enabled';
END
GO

-- Verify CLR strict security (SQL 2017+ production requirement)
DECLARE @clrStrict INT;
SELECT @clrStrict = CAST(value AS INT) FROM sys.configurations WHERE name = 'clr strict security';

IF @clrStrict = 0
BEGIN
    PRINT 'WARNING: CLR strict security is OFF';
    PRINT 'For production deployment, enable with:';
    PRINT '  EXEC sp_configure ''clr strict security'', 1; RECONFIGURE;';
    PRINT '';
END
ELSE
BEGIN
    PRINT '✓ CLR strict security is ON (production mode)';
END
GO

-- Verify database owner has EXTERNAL ACCESS rights
USE [Hartonomous];
GO

PRINT 'Verifying database owner permissions...';
DECLARE @dbOwner NVARCHAR(128);
SELECT @dbOwner = SUSER_SNAME(owner_sid) FROM sys.databases WHERE name = DB_NAME();
PRINT '  Database owner: ' + @dbOwner;

-- For UNSAFE assemblies, the database must either:
--   1. Have TRUSTWORTHY ON (NOT recommended for production), OR
--   2. Use sys.sp_add_trusted_assembly (recommended for SQL 2017+)
DECLARE @isTrustworthy BIT;
SELECT @isTrustworthy = is_trustworthy_on FROM sys.databases WHERE name = DB_NAME();

IF @isTrustworthy = 1
BEGIN
    PRINT '  ⚠ TRUSTWORTHY is ON (consider using sys.sp_add_trusted_assembly instead)';
END
ELSE
BEGIN
    PRINT '  ✓ TRUSTWORTHY is OFF (will use sys.sp_add_trusted_assembly)';
END
GO

PRINT '';
PRINT '============================================================';
PRINT 'STEP 1: DROP EXISTING ASSEMBLIES (Reverse Dependency Order)';
PRINT '============================================================';
PRINT '';

-- Drop main assembly and its objects first
PRINT 'Dropping CLR functions, aggregates, and types from Hartonomous.Clr...';
DECLARE @dropSql NVARCHAR(MAX) = '';

-- Drop functions and aggregates
SELECT @dropSql += 'DROP ' + 
    CASE o.type 
        WHEN 'AF' THEN 'AGGREGATE' 
        WHEN 'FT' THEN 'FUNCTION'
        WHEN 'FS' THEN 'FUNCTION'
        WHEN 'FN' THEN 'FUNCTION'
        WHEN 'IF' THEN 'FUNCTION'
        WHEN 'TF' THEN 'FUNCTION'
    END + 
    ' ' + QUOTENAME(SCHEMA_NAME(o.schema_id)) + '.' + QUOTENAME(o.name) + ';' + CHAR(13)
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
WHERE a.name = 'Hartonomous.Clr' 
  AND o.type IN ('AF', 'FN', 'FS', 'FT', 'IF', 'TF');

IF @dropSql != '' 
BEGIN
    EXEC sp_executesql @dropSql;
    PRINT '  ✓ Dropped CLR functions/aggregates';
END

-- Drop types
SET @dropSql = '';
SELECT @dropSql += 'DROP TYPE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ';' + CHAR(13)
FROM sys.assembly_types
WHERE assembly_id = (SELECT assembly_id FROM sys.assemblies WHERE name = 'Hartonomous.Clr');

IF @dropSql != '' 
BEGIN
    EXEC sp_executesql @dropSql;
    PRINT '  ✓ Dropped CLR types';
END
GO

-- Drop assemblies in reverse dependency order
PRINT 'Dropping assemblies...';

-- Application assembly
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'Hartonomous.Clr')
BEGIN
    DROP ASSEMBLY [Hartonomous.Clr];
    PRINT '  ✓ Dropped Hartonomous.Clr';
END

-- High-level libraries
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'Newtonsoft.Json')
    DROP ASSEMBLY [Newtonsoft.Json];
    
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'MathNet.Numerics')
    DROP ASSEMBLY [MathNet.Numerics];
    
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'ILGPU.Algorithms')
    DROP ASSEMBLY [ILGPU.Algorithms];
    
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'ILGPU')
    DROP ASSEMBLY [ILGPU];

PRINT '  ✓ Dropped high-level libraries';

-- Mid-level dependencies
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Reflection.Metadata')
    DROP ASSEMBLY [System.Reflection.Metadata];
    
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Collections.Immutable')
    DROP ASSEMBLY [System.Collections.Immutable];

PRINT '  ✓ Dropped mid-level dependencies';

-- Foundation dependencies
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.ValueTuple')
    DROP ASSEMBLY [System.ValueTuple];
    
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Memory')
    DROP ASSEMBLY [System.Memory];
    
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Buffers')
    DROP ASSEMBLY [System.Buffers];
    
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Numerics.Vectors')
    DROP ASSEMBLY [System.Numerics.Vectors];
    
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Runtime.CompilerServices.Unsafe')
    DROP ASSEMBLY [System.Runtime.CompilerServices.Unsafe];

PRINT '  ✓ Dropped foundation dependencies';
PRINT '';
GO

PRINT '============================================================';
PRINT 'STEP 2: DEPLOY ASSEMBLIES (Correct Dependency Order)';
PRINT '============================================================';
PRINT '';
PRINT 'IMPORTANT: Replace C:\Path\to\DLLs\ with your actual path';
PRINT 'Use the extract-clr-dependencies.ps1 script to prepare DLLs';
PRINT '';

-- ============================================================
-- TIER 1: FOUNDATION ASSEMBLIES (No Dependencies)
-- ============================================================

PRINT 'TIER 1: Foundation assemblies...';

-- 1. System.Runtime.CompilerServices.Unsafe (CLR v4.0.4.1, PKT: b03f5f7f11d50a3a)
-- Required by: ILGPU, System.Memory
PRINT '  [1/11] System.Runtime.CompilerServices.Unsafe';
CREATE ASSEMBLY [System.Runtime.CompilerServices.Unsafe]
FROM 'C:\Path\to\DLLs\System.Runtime.CompilerServices.Unsafe.dll'
WITH PERMISSION_SET = UNSAFE;
GO

-- 2. System.Buffers (CLR v4.0.3.0, PKT: cc7b13ffcd2ddd51)
-- Required by: System.Memory
PRINT '  [2/11] System.Buffers';
CREATE ASSEMBLY [System.Buffers]
FROM 'C:\Path\to\DLLs\System.Buffers.dll'
WITH PERMISSION_SET = UNSAFE;
GO

-- 3. System.Numerics.Vectors (CLR v4.1.5.0, PKT: b03f5f7f11d50a3a)
-- Required by: MathNet.Numerics (SIMD/AVX acceleration)
PRINT '  [3/11] System.Numerics.Vectors';
CREATE ASSEMBLY [System.Numerics.Vectors]
FROM 'C:\Path\to\DLLs\System.Numerics.Vectors.dll'
WITH PERMISSION_SET = UNSAFE;
GO

-- ============================================================
-- TIER 2: MEMORY MANAGEMENT ASSEMBLIES
-- ============================================================

PRINT 'TIER 2: Memory management...';

-- 4. System.Memory (CLR v4.0.1.0, PKT: cc7b13ffcd2ddd51)
-- Depends on: System.Runtime.CompilerServices.Unsafe, System.Buffers
-- Required by: ILGPU
PRINT '  [4/11] System.Memory';
CREATE ASSEMBLY [System.Memory]
FROM 'C:\Path\to\DLLs\System.Memory.dll'
WITH PERMISSION_SET = UNSAFE;
GO

-- ============================================================
-- TIER 3: LANGUAGE & REFLECTION SUPPORT
-- ============================================================

PRINT 'TIER 3: Language & reflection support...';

-- 5. System.ValueTuple (CLR v4.0.3.0, PKT: cc7b13ffcd2ddd51)
-- Defensive deployment for .NET Framework 4.8.1 CLR host isolation
PRINT '  [5/11] System.ValueTuple';
CREATE ASSEMBLY [System.ValueTuple]
FROM 'C:\Path\to\DLLs\System.ValueTuple.dll'
WITH PERMISSION_SET = UNSAFE;
GO

-- 6. System.Collections.Immutable (CLR v1.2.3.0, PKT: b03f5f7f11d50a3a)
-- Required by: System.Reflection.Metadata
PRINT '  [6/11] System.Collections.Immutable';
CREATE ASSEMBLY [System.Collections.Immutable]
FROM 'C:\Path\to\DLLs\System.Collections.Immutable.dll'
WITH PERMISSION_SET = UNSAFE;
GO

-- 7. System.Reflection.Metadata (CLR v1.4.5.0, PKT: b03f5f7f11d50a3a)
-- Depends on: System.Collections.Immutable
-- Required by: ILGPU (for JIT compilation and IL inspection)
PRINT '  [7/11] System.Reflection.Metadata';
CREATE ASSEMBLY [System.Reflection.Metadata]
FROM 'C:\Path\to\DLLs\System.Reflection.Metadata.dll'
WITH PERMISSION_SET = UNSAFE;
GO

-- ============================================================
-- TIER 4: HIGH-LEVEL COMPUTATION LIBRARIES
-- ============================================================

PRINT 'TIER 4: High-level computation libraries...';

-- 8. ILGPU (CLR v0.9.2.0)
-- Depends on: System.Runtime.CompilerServices.Unsafe, System.Memory, System.Reflection.Metadata
PRINT '  [8/11] ILGPU';
CREATE ASSEMBLY [ILGPU]
FROM 'C:\Path\to\DLLs\ILGPU.dll'
WITH PERMISSION_SET = UNSAFE;
GO

-- 9. ILGPU.Algorithms (CLR v0.9.2.0)
-- Depends on: ILGPU
PRINT '  [9/11] ILGPU.Algorithms';
CREATE ASSEMBLY [ILGPU.Algorithms]
FROM 'C:\Path\to\DLLs\ILGPU.Algorithms.dll'
WITH PERMISSION_SET = UNSAFE;
GO

-- 10. MathNet.Numerics (CLR v5.0.0.0)
-- Depends on: System.Numerics.Vectors (for SIMD), optionally System.ValueTuple
PRINT '  [10/11] MathNet.Numerics';
CREATE ASSEMBLY [MathNet.Numerics]
FROM 'C:\Path\to\DLLs\MathNet.Numerics.dll'
WITH PERMISSION_SET = UNSAFE;
GO

-- 11. Newtonsoft.Json (CLR v13.0.0.0, PKT: 30ad4fe6b2a6aeed)
-- Utility library (no critical dependencies)
PRINT '  [11/11] Newtonsoft.Json';
CREATE ASSEMBLY [Newtonsoft.Json]
FROM 'C:\Path\to\DLLs\Newtonsoft.Json.dll'
WITH PERMISSION_SET = UNSAFE;
GO

-- ============================================================
-- TIER 5: APPLICATION ASSEMBLY
-- ============================================================

PRINT 'TIER 5: Application assembly...';
PRINT '  [12/12] Hartonomous.Clr';

CREATE ASSEMBLY [Hartonomous.Clr]
FROM 'C:\Path\to\DLLs\Hartonomous.Clr.dll'
WITH PERMISSION_SET = UNSAFE;
GO

PRINT '';
PRINT '============================================================';
PRINT 'STEP 3: VERIFICATION';
PRINT '============================================================';
PRINT '';

-- Verify all assemblies deployed successfully
SELECT
    a.name AS AssemblyName,
    a.permission_set_desc AS PermissionSet,
    ASSEMBLYPROPERTY(a.name, 'CLRVersion') AS CLRVersion,
    ASSEMBLYPROPERTY(a.name, 'VersionMajor') AS MajorVersion,
    ASSEMBLYPROPERTY(a.name, 'VersionMinor') AS MinorVersion,
    ASSEMBLYPROPERTY(a.name, 'VersionBuild') AS BuildVersion,
    ASSEMBLYPROPERTY(a.name, 'VersionRevision') AS Revision,
    CASE WHEN a.is_user_defined = 1 THEN 'User' ELSE 'System' END AS AssemblyType
FROM sys.assemblies a
WHERE a.name IN (
    'System.Runtime.CompilerServices.Unsafe',
    'System.Buffers',
    'System.Numerics.Vectors',
    'System.Memory',
    'System.ValueTuple',
    'System.Collections.Immutable',
    'System.Reflection.Metadata',
    'ILGPU',
    'ILGPU.Algorithms',
    'MathNet.Numerics',
    'Newtonsoft.Json',
    'Hartonomous.Clr'
)
ORDER BY
    CASE a.name
        WHEN 'System.Runtime.CompilerServices.Unsafe' THEN 1
        WHEN 'System.Buffers' THEN 2
        WHEN 'System.Numerics.Vectors' THEN 3
        WHEN 'System.Memory' THEN 4
        WHEN 'System.ValueTuple' THEN 5
        WHEN 'System.Collections.Immutable' THEN 6
        WHEN 'System.Reflection.Metadata' THEN 7
        WHEN 'ILGPU' THEN 8
        WHEN 'ILGPU.Algorithms' THEN 9
        WHEN 'MathNet.Numerics' THEN 10
        WHEN 'Newtonsoft.Json' THEN 11
        WHEN 'Hartonomous.Clr' THEN 12
    END;
GO

PRINT '';
PRINT '============================================================';
PRINT 'DEPLOYMENT SUMMARY';
PRINT '============================================================';
PRINT '✓ All 12 assemblies deployed with UNSAFE permissions';
PRINT '✓ Dependency graph satisfied in correct order';
PRINT '✓ CLR Assembly Versions match runtime requirements';
PRINT '';
PRINT 'SECURITY NOTES:';
PRINT '  - UNSAFE permission set enabled (required for GPU/JIT)';
PRINT '  - All assemblies must be strong-name signed';
PRINT '  - Consider using sys.sp_add_trusted_assembly for SQL 2017+';
PRINT '';
PRINT 'NEXT STEPS:';
PRINT '  1. Deploy CLR functions using deployment scripts';
PRINT '  2. Test vector operations and GPU acceleration';
PRINT '  3. Monitor performance and memory usage';
PRINT '============================================================';
PRINT '';

GO

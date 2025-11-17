/*
================================================================================
Post-Deployment Script - CLR Assembly Deployment and Configuration
================================================================================
Purpose:
  1. Enable TRUSTWORTHY database property (required for UNSAFE CLR assemblies)
  2. Deploy external CLR dependency assemblies in proper tier order
  3. Ensure idempotent deployment (safe to run multiple times)

Execution Context:
  - Runs AFTER DACPAC schema deployment
  - Requires sysadmin or db_owner permissions
  - Requires CLR integration enabled at server level (see Pre-Deployment script)

SqlPackage Variables:
  - $(DatabaseName): Target database name (required)
  - $(DependenciesPath): Full path to DLL files directory (required)
    Example: SqlPackage.exe /v:DependenciesPath="D:\dependencies"

Deployment Tiers:
  Tier 1: Base runtime (System.Numerics.Vectors, System.ValueTuple)
  Tier 2: Memory management (System.Memory, System.Buffers, Unsafe)
  Tier 3: Collections (System.Collections.Immutable, Reflection.Metadata)
  Tier 4: Serialization (System.Runtime.Serialization, ServiceModel, SMDiagnostics)
  Tier 5: Third-party (MathNet.Numerics, Newtonsoft.Json, SqlServer.Types)
  Tier 6: Application (System.Drawing, SqlClrFunctions, Hartonomous.Database)

Idempotency:
  - All tier scripts use IF NOT EXISTS checks
  - Safe to run repeatedly
  - Will skip assemblies that already exist
  - Use /p:NoAlterStatementsToChangeClrTypes=True in SqlPackage for full DROP/CREATE
================================================================================
*/

PRINT '================================================================================';
PRINT 'POST-DEPLOYMENT: CLR Assembly Configuration';
PRINT '================================================================================';
PRINT '';
GO

-- ============================================================================
-- Step 1: Enable TRUSTWORTHY (Required for UNSAFE CLR Assemblies)
-- ============================================================================
USE [master];
GO

PRINT 'Step 1: Configuring TRUSTWORTHY property...';

DECLARE @dbName SYSNAME = '$(DatabaseName)';
DECLARE @isTrustworthy BIT;
SELECT @isTrustworthy = is_trustworthy_on FROM sys.databases WHERE name = @dbName;

IF @isTrustworthy = 0
BEGIN
    PRINT '  Setting TRUSTWORTHY ON for [' + @dbName + ']...';
    DECLARE @sql NVARCHAR(MAX) = 'ALTER DATABASE ' + QUOTENAME(@dbName) + ' SET TRUSTWORTHY ON;';
    EXEC sp_executesql @sql;
    PRINT '  ✓ TRUSTWORTHY enabled';
END
ELSE
    PRINT '  ○ TRUSTWORTHY already enabled for [' + @dbName + ']';

PRINT '';
GO

-- Return to target database
USE [$(DatabaseName)];
GO

-- ============================================================================
-- Step 2: Deploy External CLR Dependencies (6 Tiers, 16 Assemblies)
-- ============================================================================
PRINT 'Step 2: Deploying external CLR assembly dependencies...';
PRINT '  DependenciesPath: $(DependenciesPath)';
PRINT '';
GO

-- Tier 1: Base runtime dependencies (2 assemblies)
:r .\Deploy-Assembly-Tier1.sql

-- Tier 2: Memory management (3 assemblies)
:r .\Deploy-Assembly-Tier2.sql

-- Tier 3: Collections and reflection (2 assemblies)
:r .\Deploy-Assembly-Tier3.sql

-- Tier 4: Serialization (3 assemblies)
:r .\Deploy-Assembly-Tier4.sql

-- Tier 5: Third-party libraries (3 assemblies)
:r .\Deploy-Assembly-Tier5.sql

-- Tier 6: Application assemblies (3 assemblies)
:r .\Deploy-Assembly-Tier6.sql

PRINT '';
PRINT '✓ External CLR assemblies deployed successfully';
PRINT '';
GO

PRINT '================================================================================';
PRINT 'POST-DEPLOYMENT COMPLETE: Database ready for use';
PRINT '================================================================================';
GO
/*
================================================================================
Post-Deployment Script - CLR Assembly Deployment and Configuration
Idempotent: Safe to run multiple times
================================================================================
*/

PRINT 'Starting post-deployment configuration...';
GO

-- ============================================================================
-- Set TRUSTWORTHY ON (required for UNSAFE CLR assemblies)
-- ============================================================================
USE [master];
GO

DECLARE @dbName SYSNAME = '$(DatabaseName)';
DECLARE @isTrustworthy BIT;
SELECT @isTrustworthy = is_trustworthy_on FROM sys.databases WHERE name = @dbName;

IF @isTrustworthy = 0
BEGIN
    PRINT 'Setting TRUSTWORTHY ON for ' + @dbName + '...';
    DECLARE @sql NVARCHAR(MAX) = 'ALTER DATABASE ' + QUOTENAME(@dbName) + ' SET TRUSTWORTHY ON;';
    EXEC sp_executesql @sql;
    PRINT 'TRUSTWORTHY enabled.';
END
ELSE
BEGIN
    PRINT 'TRUSTWORTHY already enabled for ' + @dbName + '.';
END
GO

-- Return to target database
USE [$(DatabaseName)];
GO

-- ============================================================================
-- Deploy External CLR Assemblies (16 dependencies in tier order)
-- These are external dependencies NOT embedded in the DACPAC
-- Idempotent: IF NOT EXISTS checks prevent duplicate deployments
-- ============================================================================

PRINT 'Deploying external CLR assembly dependencies...';
GO

-- NOTE: Assembly binaries must be loaded from file system
-- This script assumes assemblies are available in a known location
-- For automated deployments, use SqlPackage with /Variables:DependenciesPath

-- Tier 1: Base runtime dependencies
:r .\Deploy-Assembly-Tier1.sql

-- Tier 2: Memory management  
:r .\Deploy-Assembly-Tier2.sql

-- Tier 3: Collections and reflection
:r .\Deploy-Assembly-Tier3.sql

-- Tier 4: Serialization
:r .\Deploy-Assembly-Tier4.sql

-- Tier 5: Third-party libraries
:r .\Deploy-Assembly-Tier5.sql

-- Tier 6: Application assemblies
:r .\Deploy-Assembly-Tier6.sql

PRINT 'External CLR assemblies deployed successfully.';
GO

PRINT 'Post-deployment configuration complete.';
PRINT 'Database ready for use.';
GO
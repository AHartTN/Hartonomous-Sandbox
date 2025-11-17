/*
================================================================================
Pre-Deployment Script - Server Configuration
================================================================================
Purpose:
  1. Enable CLR integration at SQL Server instance level
  2. Disable CLR strict security (required for UNSAFE assemblies)
  3. Enable Service Broker for async messaging

Execution Context:
  - Runs BEFORE DACPAC schema deployment
  - Requires sysadmin permissions (modifies server configuration)
  - Changes are instance-wide, not database-specific

Server Configuration Changes:
  - clr enabled = 1 (allows CLR code execution)
  - clr strict security = 0 (allows UNSAFE assemblies without signing)
  
Database Configuration Changes:
  - Service Broker enabled (for async queue processing)

Idempotency:
  - Safe to run multiple times
  - Checks current state before making changes
  - Skips configuration if already correct

Security Note:
  - CLR strict security = 0 is required for UNSAFE assemblies
  - Production: Consider using signed assemblies with sys.sp_add_trusted_assembly
  - TRUSTWORTHY is enabled in post-deployment (database-level)
================================================================================
*/

PRINT '================================================================================'
PRINT 'PRE-DEPLOYMENT: Server Configuration'
PRINT '================================================================================'
PRINT ''
GO

-- ============================================================================
-- Step 1: Enable CLR Integration (Server-Level)
-- ============================================================================
USE [master];
GO

PRINT 'Step 1: Enabling CLR integration...'

DECLARE @clrEnabled INT;
SELECT @clrEnabled = CAST(value_in_use AS INT) 
FROM sys.configurations 
WHERE name = 'clr enabled';

IF @clrEnabled = 0
BEGIN
    PRINT '  Configuring: clr enabled = 1'
    EXEC sp_configure 'show advanced options', 1;
    RECONFIGURE;
    EXEC sp_configure 'clr enabled', 1;
    RECONFIGURE;
    PRINT '  ✓ CLR integration enabled'
END
ELSE
    PRINT '  ○ CLR integration already enabled'

PRINT ''
GO

-- ============================================================================
-- Step 2: Disable CLR Strict Security (Required for UNSAFE Assemblies)
-- ============================================================================
PRINT 'Step 2: Configuring CLR strict security...'

DECLARE @clrStrictSecurity INT;
SELECT @clrStrictSecurity = CAST(value_in_use AS INT) 
FROM sys.configurations 
WHERE name = 'clr strict security';

IF @clrStrictSecurity = 1
BEGIN
    PRINT '  Configuring: clr strict security = 0'
    PRINT '  Note: Required for UNSAFE assemblies. Consider signed assemblies for production.'
    EXEC sp_configure 'show advanced options', 1;
    RECONFIGURE;
    EXEC sp_configure 'clr strict security', 0;
    RECONFIGURE;
    PRINT '  ✓ CLR strict security disabled'
END
ELSE
    PRINT '  ○ CLR strict security already disabled'

PRINT ''
GO

-- ============================================================================
-- Step 3: Enable Service Broker (Database-Level)
-- ============================================================================
USE [$(DatabaseName)];
GO

PRINT 'Step 3: Enabling Service Broker...'

IF NOT EXISTS (
    SELECT 1 
    FROM sys.databases 
    WHERE name = DB_NAME() 
      AND is_broker_enabled = 1
)
BEGIN
    PRINT '  Configuring: Service Broker enabled'
    DECLARE @sql NVARCHAR(MAX) = 'ALTER DATABASE ' + QUOTENAME(DB_NAME()) + ' SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE';
    EXEC sp_executesql @sql;
    PRINT '  ✓ Service Broker enabled'
END
ELSE
    PRINT '  ○ Service Broker already enabled'

PRINT ''
GO

-- ============================================================================
-- Step 4: Create MEMORY_OPTIMIZED_DATA Filegroup (Required for In-Memory OLTP)
-- ============================================================================
PRINT 'Step 4: Creating MEMORY_OPTIMIZED_DATA filegroup...'
PRINT '  Note: Required BEFORE any MEMORY_OPTIMIZED tables can be created'

IF NOT EXISTS (
    SELECT 1 
    FROM sys.filegroups 
    WHERE name = N'HartonomousMemoryOptimized' 
      AND type = 'FX'
)
BEGIN
    PRINT '  [4a] Creating filegroup: HartonomousMemoryOptimized'
    
    ALTER DATABASE CURRENT
    ADD FILEGROUP HartonomousMemoryOptimized CONTAINS MEMORY_OPTIMIZED_DATA;
    
    PRINT '      ✓ Filegroup created'
END
ELSE
    PRINT '  [4a] ○ Filegroup HartonomousMemoryOptimized already exists'

-- Add file to MEMORY_OPTIMIZED_DATA filegroup
IF NOT EXISTS (
    SELECT 1 
    FROM sys.database_files df
    INNER JOIN sys.filegroups fg ON df.data_space_id = fg.data_space_id
    WHERE fg.name = N'HartonomousMemoryOptimized' 
      AND fg.type = 'FX'
)
BEGIN
    PRINT '  [4b] Adding file to filegroup'
    PRINT '      Location: $(DefaultDataPath)$(DatabaseName)_InMemory'
    
    ALTER DATABASE CURRENT
    ADD FILE (
        NAME = N'HartonomousMemoryOptimized_File',
        FILENAME = N'$(DefaultDataPath)$(DatabaseName)_InMemory'
    ) TO FILEGROUP HartonomousMemoryOptimized;
    
    PRINT '      ✓ File added successfully'
END
ELSE
    PRINT '  [4b] ○ File already exists in filegroup'

PRINT ''
GO

PRINT '================================================================================'
PRINT 'PRE-DEPLOYMENT COMPLETE: Server configured for CLR and In-Memory OLTP'
PRINT '================================================================================'
GO

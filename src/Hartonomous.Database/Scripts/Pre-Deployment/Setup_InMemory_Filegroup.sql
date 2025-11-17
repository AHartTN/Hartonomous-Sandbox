/*
================================================================================
DEPRECATED: In-Memory OLTP Filegroup Setup
================================================================================
THIS FILE IS NO LONGER USED.

In-Memory OLTP filegroup setup has been moved to Script.PreDeployment.sql
as a REQUIRED step (not optional).

Reason:
  - Microsoft documentation explicitly states MEMORY_OPTIMIZED_DATA filegroup
    MUST exist BEFORE any memory-optimized tables can be created
  - SqlPackage.exe (v18.0+) has specific handling for memory-optimized filegroups
  - The system has 4 memory-optimized tables that require this filegroup:
    * BillingUsageLedger_InMemory (high-throughput billing)
    * CachedActivations_InMemory (neural network activation cache)
    * InferenceCache_InMemory (inference result cache)
    * SessionPaths_InMemory (behavioral analysis)

Reference:
  - SqlPackage v18.0 Release Notes: "Fixed creating a database with memory
    optimized file groups when memory optimized tables are used."
  - https://learn.microsoft.com/sql/relational-databases/in-memory-oltp/the-memory-optimized-filegroup
  - DACPAC deployment order: Pre-Deployment script runs BEFORE table creation

This file is retained only for historical reference and will be removed in
future cleanup.
================================================================================
*/

USE [$(DatabaseName)];
GO

PRINT 'Optional Feature: In-Memory OLTP Filegroup Setup';
PRINT '';
GO

-- ============================================================================
-- Step 1: Create MEMORY_OPTIMIZED_DATA Filegroup
-- ============================================================================
PRINT '  [1/2] Creating MEMORY_OPTIMIZED_DATA filegroup...';

IF NOT EXISTS (
    SELECT 1 
    FROM sys.filegroups 
    WHERE name = N'HartonomousMemoryOptimized' 
      AND type = 'FX'
)
BEGIN
    PRINT '        Executing: ADD FILEGROUP HartonomousMemoryOptimized';
    
    ALTER DATABASE CURRENT
    ADD FILEGROUP HartonomousMemoryOptimized CONTAINS MEMORY_OPTIMIZED_DATA;
    
    PRINT '        ✓ Filegroup created successfully';
END
ELSE
    PRINT '        ○ Filegroup HartonomousMemoryOptimized already exists';

PRINT '';
GO

-- ============================================================================
-- Step 2: Add File to MEMORY_OPTIMIZED_DATA Filegroup
-- ============================================================================
PRINT '  [2/2] Adding file to MEMORY_OPTIMIZED_DATA filegroup...';

IF NOT EXISTS (
    SELECT 1 
    FROM sys.database_files df
    INNER JOIN sys.filegroups fg ON df.data_space_id = fg.data_space_id
    WHERE fg.name = N'HartonomousMemoryOptimized' 
      AND fg.type = 'FX'
)
BEGIN
    PRINT '        Executing: ADD FILE HartonomousMemoryOptimized_File';
    PRINT '        Location: $(DefaultDataPath)$(DatabaseName)_InMemory';
    
    ALTER DATABASE CURRENT
    ADD FILE (
        NAME = N'HartonomousMemoryOptimized_File',
        FILENAME = N'$(DefaultDataPath)$(DatabaseName)_InMemory'
    ) TO FILEGROUP HartonomousMemoryOptimized;
    
    PRINT '        ✓ File added successfully';
END
ELSE
    PRINT '        ○ File already exists in filegroup';

PRINT '';
PRINT '✓ In-Memory OLTP filegroup setup complete';
PRINT 'Ready for MEMORY_OPTIMIZED table creation';
PRINT '';
GO

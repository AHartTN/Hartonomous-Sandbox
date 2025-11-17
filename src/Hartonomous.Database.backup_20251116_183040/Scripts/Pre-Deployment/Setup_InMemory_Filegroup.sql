/*
================================================================================
In-Memory OLTP Filegroup Setup
================================================================================
Purpose: Creates MEMORY_OPTIMIZED_DATA filegroup required for In-Memory OLTP
         tables (BillingUsageLedger_InMemory).

Requirements:
- Runs BEFORE DACPAC deployment
- Environment-specific: Requires physical directory path
- SQL Server 2014+ with In-Memory OLTP feature enabled

Notes:
- Uses SQLCMD variable $(DefaultDataPath) for environment-specific paths
- Filegroup type 'FX' = MEMORY_OPTIMIZED_DATA
- Tables with MEMORY_OPTIMIZED = ON require this filegroup to exist BEFORE creation
================================================================================
*/

PRINT 'Setting up In-Memory OLTP filegroup...';

-- Check if filegroup already exists
IF NOT EXISTS (
    SELECT 1 
    FROM sys.filegroups 
    WHERE name = N'HartonomousMemoryOptimized' 
      AND type = 'FX'
)
BEGIN
    PRINT '  Creating MEMORY_OPTIMIZED_DATA filegroup: HartonomousMemoryOptimized';
    
    ALTER DATABASE CURRENT
    ADD FILEGROUP HartonomousMemoryOptimized CONTAINS MEMORY_OPTIMIZED_DATA;
    
    PRINT '  ✓ Filegroup created';
END
ELSE
    PRINT '  ○ Filegroup HartonomousMemoryOptimized already exists';

-- Check if file already exists
IF NOT EXISTS (
    SELECT 1 
    FROM sys.database_files df
    INNER JOIN sys.filegroups fg ON df.data_space_id = fg.data_space_id
    WHERE fg.name = N'HartonomousMemoryOptimized' 
      AND fg.type = 'FX'
)
BEGIN
    PRINT '  Adding file to HartonomousMemoryOptimized filegroup';
    
    ALTER DATABASE CURRENT
    ADD FILE (
        NAME = N'HartonomousMemoryOptimized_File',
        FILENAME = N'$(DefaultDataPath)$(DatabaseName)_InMemory'
    ) TO FILEGROUP HartonomousMemoryOptimized;
    
    PRINT '  ✓ File added to filegroup';
END
ELSE
    PRINT '  ○ File already exists in filegroup';

PRINT '✓ In-Memory OLTP filegroup setup complete';
PRINT '';

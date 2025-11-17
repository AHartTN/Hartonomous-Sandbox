USE [$(DatabaseName)]
GO

-- Step 2: Enable FILESTREAM on database
IF NOT EXISTS (
    SELECT 1 
    FROM sys.filegroups 
    WHERE name = N'HartonomousFileStream' AND type = 'FD'
)
BEGIN
    -- Add FILESTREAM filegroup
    ALTER DATABASE [$(DatabaseName)]
    ADD FILEGROUP HartonomousFileStream CONTAINS FILESTREAM;
    
    PRINT 'FILESTREAM filegroup created.';
END
ELSE
BEGIN
    PRINT 'FILESTREAM filegroup already exists.';
END
GO

-- Add file to filegroup using SQL Server data directory
-- This path needs to be configured based on the environment.
-- For DACPAC deployment, this might be handled differently or require manual configuration.
-- For now, we'll include a placeholder.
IF NOT EXISTS (
    SELECT 1 
    FROM sys.database_files 
    WHERE name = N'HartonomousFileStream_File' AND type = 2 -- FILESTREAM data
)
BEGIN
    -- IMPORTANT: Replace 'D:\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\DATA\HartonomousFileStream'
    -- with the actual path on your SQL Server instance.
    -- This path must exist and be accessible by the SQL Server service account.
    ALTER DATABASE [$(DatabaseName)]
    ADD FILE (
        NAME = N'HartonomousFileStream_File',
        FILENAME = N'$(DefaultDataPath)$(DatabaseName)_HartonomousFileStream' -- Placeholder for DACPAC
    ) TO FILEGROUP HartonomousFileStream;
    
    PRINT 'FILESTREAM file added to filegroup.';
END
ELSE
BEGIN
    PRINT 'FILESTREAM file already exists.';
END
GO
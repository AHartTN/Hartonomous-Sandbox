-- =============================================
-- File System CLR Bindings for Autonomous Deployment
-- =============================================
-- Provides file I/O and shell command execution
-- Required for sp_AutonomousImprovement Phase 4 (deployment)
-- =============================================

USE Hartonomous;
GO

-- Write bytes to file
IF OBJECT_ID('dbo.clr_WriteFileBytes', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_WriteFileBytes;
GO
CREATE FUNCTION dbo.clr_WriteFileBytes(
    @FilePath NVARCHAR(MAX),
    @Content VARBINARY(MAX)
)
RETURNS BIGINT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].WriteFileBytes;
GO

-- Write UTF-8 text to file
IF OBJECT_ID('dbo.clr_WriteFileText', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_WriteFileText;
GO
CREATE FUNCTION dbo.clr_WriteFileText(
    @FilePath NVARCHAR(MAX),
    @Content NVARCHAR(MAX)
)
RETURNS BIGINT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].WriteFileText;
GO

-- Read bytes from file
IF OBJECT_ID('dbo.clr_ReadFileBytes', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_ReadFileBytes;
GO
CREATE FUNCTION dbo.clr_ReadFileBytes(
    @FilePath NVARCHAR(MAX)
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].ReadFileBytes;
GO

-- Read UTF-8 text from file
IF OBJECT_ID('dbo.clr_ReadFileText', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_ReadFileText;
GO
CREATE FUNCTION dbo.clr_ReadFileText(
    @FilePath NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].ReadFileText;
GO

-- Execute shell command and return output
IF OBJECT_ID('dbo.clr_ExecuteShellCommand', 'TF') IS NOT NULL DROP FUNCTION dbo.clr_ExecuteShellCommand;
GO
CREATE FUNCTION dbo.clr_ExecuteShellCommand(
    @Command NVARCHAR(MAX),
    @WorkingDirectory NVARCHAR(MAX),
    @TimeoutSeconds INT
)
RETURNS TABLE (
    OutputLine NVARCHAR(MAX),
    IsError BIT
)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].ExecuteShellCommand;
GO

-- Check if file exists
IF OBJECT_ID('dbo.clr_FileExists', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_FileExists;
GO
CREATE FUNCTION dbo.clr_FileExists(
    @FilePath NVARCHAR(MAX)
)
RETURNS BIT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].FileExists;
GO

-- Check if directory exists
IF OBJECT_ID('dbo.clr_DirectoryExists', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_DirectoryExists;
GO
CREATE FUNCTION dbo.clr_DirectoryExists(
    @DirectoryPath NVARCHAR(MAX)
)
RETURNS BIT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].DirectoryExists;
GO

-- Delete file
IF OBJECT_ID('dbo.clr_DeleteFile', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_DeleteFile;
GO
CREATE FUNCTION dbo.clr_DeleteFile(
    @FilePath NVARCHAR(MAX)
)
RETURNS BIT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].DeleteFile;
GO

PRINT 'File system CLR bindings created successfully';
PRINT 'Functions: clr_WriteFileBytes, clr_WriteFileText, clr_ReadFileBytes, clr_ReadFileText, clr_ExecuteShellCommand, clr_FileExists, clr_DirectoryExists, clr_DeleteFile';
GO

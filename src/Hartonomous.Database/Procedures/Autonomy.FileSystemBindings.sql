-- =============================================
-- File System CLR Bindings for Autonomous Deployment
-- =============================================
-- Provides file I/O and shell command execution
-- Required for sp_AutonomousImprovement Phase 4 (deployment)
-- =============================================

-- Write bytes to file

CREATE FUNCTION dbo.clr_WriteFileBytes(
    @FilePath NVARCHAR(MAX),
    @Content VARBINARY(MAX)
)
RETURNS BIGINT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].WriteFileBytes;

-- Write UTF-8 text to file

CREATE FUNCTION dbo.clr_WriteFileText(
    @FilePath NVARCHAR(MAX),
    @Content NVARCHAR(MAX)
)
RETURNS BIGINT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].WriteFileText;

-- Read bytes from file

CREATE FUNCTION dbo.clr_ReadFileBytes(
    @FilePath NVARCHAR(MAX)
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].ReadFileBytes;

-- Read UTF-8 text from file

CREATE FUNCTION dbo.clr_ReadFileText(
    @FilePath NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].ReadFileText;

-- Execute shell command and return output
-- SECURITY: Updated signature to prevent command injection

CREATE FUNCTION dbo.clr_ExecuteShellCommand(
    @Executable NVARCHAR(MAX),
    @Arguments NVARCHAR(MAX),
    @WorkingDirectory NVARCHAR(MAX),
    @TimeoutSeconds INT
)
RETURNS TABLE (
    OutputLine NVARCHAR(MAX),
    IsError BIT
)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].ExecuteShellCommand;

-- Check if file exists

CREATE FUNCTION dbo.clr_FileExists(
    @FilePath NVARCHAR(MAX)
)
RETURNS BIT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].FileExists;

-- Check if directory exists

CREATE FUNCTION dbo.clr_DirectoryExists(
    @DirectoryPath NVARCHAR(MAX)
)
RETURNS BIT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].DirectoryExists;

-- Delete file

CREATE FUNCTION dbo.clr_DeleteFile(
    @FilePath NVARCHAR(MAX)
)
RETURNS BIT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].DeleteFile;

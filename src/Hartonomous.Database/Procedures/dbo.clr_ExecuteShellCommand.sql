CREATE FUNCTION dbo.clr_ExecuteShellCommand(
    @executable NVARCHAR(MAX),
    @arguments NVARCHAR(MAX),
    @workingDirectory NVARCHAR(MAX),
    @timeoutSeconds INT
)
RETURNS TABLE (
    OutputLine NVARCHAR(MAX),
    IsError BIT
)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].ExecuteShellCommand;
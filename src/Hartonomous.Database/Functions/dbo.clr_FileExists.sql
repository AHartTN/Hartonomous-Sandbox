CREATE FUNCTION dbo.clr_FileExists(@filePath NVARCHAR(MAX))
RETURNS BIT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].FileExists;
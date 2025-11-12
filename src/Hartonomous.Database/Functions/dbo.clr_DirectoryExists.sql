CREATE FUNCTION dbo.clr_DirectoryExists(@directoryPath NVARCHAR(MAX))
RETURNS BIT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].DirectoryExists;
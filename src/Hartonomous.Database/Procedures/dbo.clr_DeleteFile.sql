CREATE FUNCTION dbo.clr_DeleteFile(@filePath NVARCHAR(MAX))
RETURNS BIT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].DeleteFile;
CREATE FUNCTION dbo.clr_ReadFileBytes(@filePath NVARCHAR(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].ReadFileBytes;
CREATE FUNCTION dbo.clr_WriteFileBytes(@filePath NVARCHAR(MAX), @content VARBINARY(MAX))
RETURNS BIGINT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].WriteFileBytes;
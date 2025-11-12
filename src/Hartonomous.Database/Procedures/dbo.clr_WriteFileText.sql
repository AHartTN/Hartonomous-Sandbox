CREATE FUNCTION dbo.clr_WriteFileText(@filePath NVARCHAR(MAX), @content NVARCHAR(MAX))
RETURNS BIGINT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].WriteFileText;
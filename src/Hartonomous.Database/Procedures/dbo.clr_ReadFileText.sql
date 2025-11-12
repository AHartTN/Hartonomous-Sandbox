CREATE FUNCTION dbo.clr_ReadFileText(@filePath NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.FileSystemFunctions].ReadFileText;
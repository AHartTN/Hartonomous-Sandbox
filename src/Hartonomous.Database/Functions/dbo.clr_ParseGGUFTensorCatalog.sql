CREATE FUNCTION dbo.clr_ParseGGUFTensorCatalog(@modelBlob VARBINARY(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ModelIngestionFunctions].ParseGGUFTensorCatalog;
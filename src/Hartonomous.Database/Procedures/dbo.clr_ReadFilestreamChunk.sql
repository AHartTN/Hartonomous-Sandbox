CREATE FUNCTION dbo.clr_ReadFilestreamChunk(@filestreamPath NVARCHAR(MAX), @offset BIGINT, @length INT)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ModelIngestionFunctions].ReadFilestreamChunk;
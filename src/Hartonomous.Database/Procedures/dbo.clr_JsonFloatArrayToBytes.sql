CREATE FUNCTION dbo.clr_JsonFloatArrayToBytes(@jsonFloatArray NVARCHAR(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.TensorDataIO].clr_JsonFloatArrayToBytes;
CREATE FUNCTION dbo.clr_BytesToFloatArrayJson(@bytes VARBINARY(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.TensorDataIO].clr_BytesToFloatArrayJson;
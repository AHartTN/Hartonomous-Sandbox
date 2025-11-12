CREATE FUNCTION dbo.clr_GetTensorAtomPayload(@tensorAtomId BIGINT)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.TensorDataIO].clr_GetTensorAtomPayload;
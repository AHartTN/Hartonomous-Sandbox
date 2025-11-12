CREATE PROCEDURE dbo.clr_StoreTensorAtomPayload
    @tensorAtomId BIGINT,
    @payload VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.TensorDataIO].clr_StoreTensorAtomPayload;
CREATE FUNCTION dbo.clr_RunInference(@modelId INT, @tokenIdsJson NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.TensorOperations.TransformerInference].clr_RunInference;
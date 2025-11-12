CREATE FUNCTION dbo.clr_ComputeSemanticFeatures(@text NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SemanticAnalysis].ComputeSemanticFeatures;
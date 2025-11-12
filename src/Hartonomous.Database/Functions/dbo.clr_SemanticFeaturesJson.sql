CREATE FUNCTION dbo.clr_SemanticFeaturesJson(@text NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SemanticAnalysis].ComputeSemanticFeatures;
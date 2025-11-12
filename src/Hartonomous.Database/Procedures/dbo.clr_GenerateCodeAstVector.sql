CREATE FUNCTION dbo.clr_GenerateCodeAstVector(@sourceCode NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.CodeAnalysis].clr_GenerateCodeAstVector;
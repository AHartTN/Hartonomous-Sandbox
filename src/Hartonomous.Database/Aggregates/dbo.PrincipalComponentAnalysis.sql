CREATE AGGREGATE dbo.PrincipalComponentAnalysis(@vector NVARCHAR(MAX), @numComponents INT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.PrincipalComponentAnalysis];
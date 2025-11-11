CREATE AGGREGATE dbo.ABTestAnalysis(@variant NVARCHAR(MAX), @metric FLOAT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.ABTestAnalysis];
GO
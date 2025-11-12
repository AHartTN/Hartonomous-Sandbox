CREATE AGGREGATE dbo.GradientStatistics(@gradient NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.GradientStatistics];
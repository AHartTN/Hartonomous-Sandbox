CREATE AGGREGATE dbo.ChurnPrediction(@featureVector NVARCHAR(MAX), @daysSinceLastActivity INT)
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.ChurnPrediction];
CREATE AGGREGATE dbo.DiversityRecommendation(@itemVector NVARCHAR(MAX), @diversityWeight FLOAT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.DiversityRecommendation];
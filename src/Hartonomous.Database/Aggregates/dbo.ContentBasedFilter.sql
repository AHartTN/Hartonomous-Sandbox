CREATE AGGREGATE dbo.ContentBasedFilter(@itemFeatures NVARCHAR(MAX), @userPreferences NVARCHAR(MAX))
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.ContentBasedFilter];
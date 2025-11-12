CREATE AGGREGATE dbo.MahalanobisDistance(@vector NVARCHAR(MAX))
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.MahalanobisDistance];
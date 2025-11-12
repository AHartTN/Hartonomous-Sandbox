CREATE AGGREGATE dbo.ReflexionAggregate(@trajectory NVARCHAR(MAX), @feedback NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.ReflexionAggregate];
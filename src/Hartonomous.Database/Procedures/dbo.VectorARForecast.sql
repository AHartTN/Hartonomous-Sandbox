CREATE AGGREGATE dbo.VectorARForecast(@vector NVARCHAR(MAX), @lag INT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.VectorARForecast];
GO
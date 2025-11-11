CREATE AGGREGATE dbo.DTWDistance(@series1 NVARCHAR(MAX), @series2 NVARCHAR(MAX))
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.DTWDistance];
GO
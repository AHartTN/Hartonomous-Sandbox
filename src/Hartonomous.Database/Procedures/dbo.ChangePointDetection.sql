CREATE AGGREGATE dbo.ChangePointDetection(@value FLOAT, @timestamp DATETIME2)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.ChangePointDetection];
GO
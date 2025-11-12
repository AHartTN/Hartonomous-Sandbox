CREATE AGGREGATE dbo.VectorDriftOverTime(@vector NVARCHAR(MAX), @timestamp DATETIME2)
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.VectorDriftOverTime];
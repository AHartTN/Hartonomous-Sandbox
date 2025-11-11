CREATE AGGREGATE dbo.CosineAnnealingSchedule(@iteration INT, @maxIterations INT)
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.CosineAnnealingSchedule];
GO
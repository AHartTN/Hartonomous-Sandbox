CREATE AGGREGATE dbo.IsolationForestScore(@vector NVARCHAR(MAX), @numTrees INT)
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.IsolationForestScore];
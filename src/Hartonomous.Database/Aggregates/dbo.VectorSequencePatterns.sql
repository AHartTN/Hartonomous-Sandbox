CREATE AGGREGATE dbo.VectorSequencePatterns(@vector NVARCHAR(MAX), @sequenceIndex INT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.VectorSequencePatterns];
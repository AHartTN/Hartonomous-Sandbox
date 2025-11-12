CREATE AGGREGATE dbo.TreeOfThought(@hypothesis NVARCHAR(MAX), @score FLOAT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.TreeOfThought];
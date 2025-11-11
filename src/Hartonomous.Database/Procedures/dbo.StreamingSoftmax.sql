CREATE AGGREGATE dbo.StreamingSoftmax(@vector NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.StreamingSoftmax];
GO
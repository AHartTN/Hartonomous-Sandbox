CREATE AGGREGATE dbo.CollaborativeFilter(@userId INT, @itemVector NVARCHAR(MAX), @rating FLOAT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.CollaborativeFilter];
GO
CREATE AGGREGATE dbo.SelfConsistency(@reasoning NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.SelfConsistency];
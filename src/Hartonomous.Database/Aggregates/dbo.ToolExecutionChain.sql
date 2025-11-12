CREATE AGGREGATE dbo.ToolExecutionChain(@toolName NVARCHAR(MAX), @executionTime INT, @success BIT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.ToolExecutionChain];
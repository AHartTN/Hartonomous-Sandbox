CREATE AGGREGATE dbo.ResearchWorkflow(@actionTaken NVARCHAR(MAX), @resultQuality FLOAT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.ResearchWorkflow];
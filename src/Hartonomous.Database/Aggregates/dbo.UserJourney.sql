CREATE AGGREGATE dbo.UserJourney(@eventType NVARCHAR(MAX), @timestamp DATETIME2, @sessionId UNIQUEIDENTIFIER)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.UserJourney];
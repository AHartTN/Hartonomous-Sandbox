USE [$(DatabaseName)]
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'AnalyzeQueue')
BEGIN
    CREATE QUEUE AnalyzeQueue WITH STATUS = ON;
END
GO
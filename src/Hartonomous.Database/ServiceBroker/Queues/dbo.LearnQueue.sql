USE [$(DatabaseName)]
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'LearnQueue')
BEGIN
    CREATE QUEUE LearnQueue WITH STATUS = ON;
END
GO
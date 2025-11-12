USE [$(DatabaseName)]
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'ActQueue')
BEGIN
    CREATE QUEUE ActQueue WITH STATUS = ON;
END
GO
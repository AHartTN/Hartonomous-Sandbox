USE [$(DatabaseName)]
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'HypothesizeQueue')
BEGIN
    CREATE QUEUE HypothesizeQueue WITH STATUS = ON;
END
GO
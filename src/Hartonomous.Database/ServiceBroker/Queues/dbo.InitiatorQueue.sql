USE [$(DatabaseName)]
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'InitiatorQueue')
BEGIN
    CREATE QUEUE InitiatorQueue WITH STATUS = ON;
END
GO
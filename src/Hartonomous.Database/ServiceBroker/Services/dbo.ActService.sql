USE [$(DatabaseName)]
GO

IF NOT EXISTS (SELECT 1 FROM sys.services WHERE name = 'ActService')
BEGIN
    CREATE SERVICE ActService ON QUEUE ActQueue (
        [//Hartonomous/AutonomousLoop/ActContract]
    );
END
GO
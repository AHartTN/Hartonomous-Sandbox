USE [$(DatabaseName)]
GO

IF NOT EXISTS (SELECT 1 FROM sys.services WHERE name = 'HypothesizeService')
BEGIN
    CREATE SERVICE HypothesizeService ON QUEUE HypothesizeQueue (
        [//Hartonomous/AutonomousLoop/HypothesizeContract]
    );
END
GO
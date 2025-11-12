USE [$(DatabaseName)]
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_contracts WHERE name = '//Hartonomous/AutonomousLoop/HypothesizeContract')
BEGIN
    CREATE CONTRACT [//Hartonomous/AutonomousLoop/HypothesizeContract] (
        [//Hartonomous/AutonomousLoop/HypothesizeMessage] SENT BY INITIATOR
    );
END
GO
USE [$(DatabaseName)]
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_contracts WHERE name = '//Hartonomous/AutonomousLoop/LearnContract')
BEGIN
    CREATE CONTRACT [//Hartonomous/AutonomousLoop/LearnContract] (
        [//Hartonomous/AutonomousLoop/LearnMessage] SENT BY INITIATOR
    );
END
GO
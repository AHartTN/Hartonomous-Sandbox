USE [$(DatabaseName)]
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_contracts WHERE name = '//Hartonomous/AutonomousLoop/ActContract')
BEGIN
    CREATE CONTRACT [//Hartonomous/AutonomousLoop/ActContract] (
        [//Hartonomous/AutonomousLoop/ActMessage] SENT BY INITIATOR
    );
END
GO
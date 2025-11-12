USE [$(DatabaseName)]
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_contracts WHERE name = '//Hartonomous/AutonomousLoop/AnalyzeContract')
BEGIN
    CREATE CONTRACT [//Hartonomous/AutonomousLoop/AnalyzeContract] (
        [//Hartonomous/AutonomousLoop/AnalyzeMessage] SENT BY INITIATOR
    );
END
GO
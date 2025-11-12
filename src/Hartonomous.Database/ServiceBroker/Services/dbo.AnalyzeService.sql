USE [$(DatabaseName)]
GO

IF NOT EXISTS (SELECT 1 FROM sys.services WHERE name = 'AnalyzeService')
BEGIN
    CREATE SERVICE AnalyzeService ON QUEUE AnalyzeQueue (
        [//Hartonomous/AutonomousLoop/AnalyzeContract]
    );
END
GO
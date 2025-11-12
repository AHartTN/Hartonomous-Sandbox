USE [$(DatabaseName)]
GO

IF NOT EXISTS (SELECT 1 FROM sys.services WHERE name = '//Hartonomous/Service/Initiator')
BEGIN
    CREATE SERVICE [//Hartonomous/Service/Initiator] ON QUEUE InitiatorQueue (
        [//Hartonomous/AutonomousLoop/AnalyzeContract],
        [//Hartonomous/AutonomousLoop/HypothesizeContract],
        [//Hartonomous/AutonomousLoop/ActContract],
        [//Hartonomous/AutonomousLoop/LearnContract]
    );
END
GO
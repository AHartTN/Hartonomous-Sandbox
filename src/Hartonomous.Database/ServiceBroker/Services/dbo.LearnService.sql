USE [$(DatabaseName)]
GO

IF NOT EXISTS (SELECT 1 FROM sys.services WHERE name = 'LearnService')
BEGIN
    CREATE SERVICE LearnService ON QUEUE LearnQueue (
        [//Hartonomous/AutonomousLoop/LearnContract]
    );
END
GO
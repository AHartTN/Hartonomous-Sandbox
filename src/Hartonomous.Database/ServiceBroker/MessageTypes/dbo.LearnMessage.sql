USE [$(DatabaseName)]
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_message_types WHERE name = '//Hartonomous/AutonomousLoop/LearnMessage')
BEGIN
    CREATE MESSAGE TYPE [//Hartonomous/AutonomousLoop/LearnMessage] VALIDATION = WELL_FORMED_XML;
END
GO
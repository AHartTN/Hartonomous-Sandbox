-- Service Broker message infrastructure for autonomous loop
-- Implements OODA-inspired: Analyze -> Hypothesize -> Act -> Learn cycle
-- Each phase has dedicated queue with activation stored procedure

USE Hartonomous;
GO

-- Message types for each phase
IF NOT EXISTS (SELECT 1 FROM sys.service_message_types WHERE name = '//Hartonomous/AutonomousLoop/AnalyzeMessage')
BEGIN
    CREATE MESSAGE TYPE [//Hartonomous/AutonomousLoop/AnalyzeMessage] VALIDATION = WELL_FORMED_XML;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_message_types WHERE name = '//Hartonomous/AutonomousLoop/HypothesizeMessage')
BEGIN
    CREATE MESSAGE TYPE [//Hartonomous/AutonomousLoop/HypothesizeMessage] VALIDATION = WELL_FORMED_XML;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_message_types WHERE name = '//Hartonomous/AutonomousLoop/ActMessage')
BEGIN
    CREATE MESSAGE TYPE [//Hartonomous/AutonomousLoop/ActMessage] VALIDATION = WELL_FORMED_XML;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_message_types WHERE name = '//Hartonomous/AutonomousLoop/LearnMessage')
BEGIN
    CREATE MESSAGE TYPE [//Hartonomous/AutonomousLoop/LearnMessage] VALIDATION = WELL_FORMED_XML;
END
GO

-- Contracts (request/reply patterns)
IF NOT EXISTS (SELECT 1 FROM sys.service_contracts WHERE name = '//Hartonomous/AutonomousLoop/AnalyzeContract')
BEGIN
    CREATE CONTRACT [//Hartonomous/AutonomousLoop/AnalyzeContract] (
        [//Hartonomous/AutonomousLoop/AnalyzeMessage] SENT BY INITIATOR
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_contracts WHERE name = '//Hartonomous/AutonomousLoop/HypothesizeContract')
BEGIN
    CREATE CONTRACT [//Hartonomous/AutonomousLoop/HypothesizeContract] (
        [//Hartonomous/AutonomousLoop/HypothesizeMessage] SENT BY INITIATOR
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_contracts WHERE name = '//Hartonomous/AutonomousLoop/ActContract')
BEGIN
    CREATE CONTRACT [//Hartonomous/AutonomousLoop/ActContract] (
        [//Hartonomous/AutonomousLoop/ActMessage] SENT BY INITIATOR
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_contracts WHERE name = '//Hartonomous/AutonomousLoop/LearnContract')
BEGIN
    CREATE CONTRACT [//Hartonomous/AutonomousLoop/LearnContract] (
        [//Hartonomous/AutonomousLoop/LearnMessage] SENT BY INITIATOR
    );
END
GO

-- Queues (will add activation procedures later)
IF NOT EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'AnalyzeQueue')
BEGIN
    CREATE QUEUE AnalyzeQueue WITH STATUS = ON;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'HypothesizeQueue')
BEGIN
    CREATE QUEUE HypothesizeQueue WITH STATUS = ON;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'ActQueue')
BEGIN
    CREATE QUEUE ActQueue WITH STATUS = ON;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'LearnQueue')
BEGIN
    CREATE QUEUE LearnQueue WITH STATUS = ON;
END
GO

-- Services
-- Initiator Service: Used by sp_StartPrimeSearch and other entry points to trigger the autonomous loop
IF NOT EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'InitiatorQueue')
BEGIN
    CREATE QUEUE InitiatorQueue WITH STATUS = ON;
END
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

IF NOT EXISTS (SELECT 1 FROM sys.services WHERE name = 'AnalyzeService')
BEGIN
    CREATE SERVICE AnalyzeService ON QUEUE AnalyzeQueue (
        [//Hartonomous/AutonomousLoop/AnalyzeContract]
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.services WHERE name = 'HypothesizeService')
BEGIN
    CREATE SERVICE HypothesizeService ON QUEUE HypothesizeQueue (
        [//Hartonomous/AutonomousLoop/HypothesizeContract]
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.services WHERE name = 'ActService')
BEGIN
    CREATE SERVICE ActService ON QUEUE ActQueue (
        [//Hartonomous/AutonomousLoop/ActContract]
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.services WHERE name = 'LearnService')
BEGIN
    CREATE SERVICE LearnService ON QUEUE LearnQueue (
        [//Hartonomous/AutonomousLoop/LearnContract]
    );
END
GO

-- Verify setup
SELECT 
    s.name AS ServiceName,
    sq.name AS QueueName,
    sq.is_activation_enabled AS ActivationEnabled,
    sq.is_receive_enabled AS ReceiveEnabled
FROM sys.services s
JOIN sys.service_queues sq ON s.service_queue_id = sq.object_id
WHERE s.name IN ('//Hartonomous/Service/Initiator', 'AnalyzeService', 'HypothesizeService', 'ActService', 'LearnService')
ORDER BY s.name;
GO

PRINT 'Service Broker infrastructure created successfully.';
PRINT 'GÖDEL ENGINE: Ready to process autonomous compute jobs.';
GO

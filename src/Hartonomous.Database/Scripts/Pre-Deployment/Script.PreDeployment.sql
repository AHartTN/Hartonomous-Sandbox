/*
Pre-Deployment Script: Schema and Service Broker Setup
Executed before DACPAC schema deployment
*/

-- Create schemas if they don't exist
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'provenance')
BEGIN
    EXEC('CREATE SCHEMA [provenance]');
    END
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'graph')
BEGIN
    EXEC('CREATE SCHEMA [graph]');
    END
GO

-- Service Broker setup
-- Enable Service Broker if not already enabled
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = DB_NAME() AND is_broker_enabled = 1)
BEGIN
    ALTER DATABASE CURRENT SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE;
END
GO

-- Create message types
IF NOT EXISTS (SELECT * FROM sys.service_message_types WHERE name = 'InferenceJobMessage')
BEGIN
    CREATE MESSAGE TYPE InferenceJobMessage VALIDATION = WELL_FORMED_XML;
    END
GO

-- Create contract
IF NOT EXISTS (SELECT * FROM sys.service_contracts WHERE name = 'InferenceJobContract')
BEGIN
    CREATE CONTRACT InferenceJobContract (
        InferenceJobMessage SENT BY ANY
    );
    END
GO

-- Create queue
IF NOT EXISTS (SELECT * FROM sys.service_queues WHERE name = 'InferenceQueue')
BEGIN
    CREATE QUEUE InferenceQueue
    WITH STATUS = ON,
         RETENTION = OFF;
    END
GO

-- Create service
IF NOT EXISTS (SELECT * FROM sys.services WHERE name = 'InferenceService')
BEGIN
    CREATE SERVICE InferenceService
    ON QUEUE InferenceQueue (InferenceJobContract);
    END
GO

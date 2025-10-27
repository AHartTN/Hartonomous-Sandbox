-- =============================================
-- Service Broker Setup: Event-Driven Architecture for Hartonomous
-- =============================================
-- Purpose: Enable asynchronous, reliable, transactional message processing
-- Use Cases: 
--   - Video frame ingestion triggers embedding generation
--   - Audio chunk arrival triggers Whisper transcription
--   - SCADA telemetry triggers anomaly detection
--   - Model weight updates trigger recomputation of downstream embeddings
-- =============================================

-- Enable Service Broker on database (if not already enabled)
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'Hartonomous' AND is_broker_enabled = 1)
BEGIN
    ALTER DATABASE Hartonomous SET ENABLE_BROKER;
    PRINT 'Service Broker enabled on Hartonomous database';
END
ELSE
BEGIN
    PRINT 'Service Broker already enabled';
END
GO

-- =============================================
-- Message Types: Define contracts for communication
-- =============================================

-- Generic sensor data message
CREATE MESSAGE TYPE [//Hartonomous/SensorData]
    VALIDATION = WELL_FORMED_XML;
GO

-- Video frame message (includes frame bytes + metadata)
CREATE MESSAGE TYPE [//Hartonomous/VideoFrame]
    VALIDATION = WELL_FORMED_XML;
GO

-- Audio chunk message (includes audio samples + metadata)
CREATE MESSAGE TYPE [//Hartonomous/AudioChunk]
    VALIDATION = WELL_FORMED_XML;
GO

-- SCADA telemetry message (includes sensor readings + timestamp)
CREATE MESSAGE TYPE [//Hartonomous/SCADAData]
    VALIDATION = WELL_FORMED_XML;
GO

-- Model update notification (triggers re-inference)
CREATE MESSAGE TYPE [//Hartonomous/ModelUpdated]
    VALIDATION = WELL_FORMED_XML;
GO

PRINT 'Message types created';
GO

-- =============================================
-- Contracts: Define which message types are allowed
-- =============================================

CREATE CONTRACT [//Hartonomous/SensorDataContract]
(
    [//Hartonomous/SensorData] SENT BY INITIATOR
);
GO

CREATE CONTRACT [//Hartonomous/VideoFrameContract]
(
    [//Hartonomous/VideoFrame] SENT BY INITIATOR
);
GO

CREATE CONTRACT [//Hartonomous/AudioChunkContract]
(
    [//Hartonomous/AudioChunk] SENT BY INITIATOR
);
GO

CREATE CONTRACT [//Hartonomous/SCADADataContract]
(
    [//Hartonomous/SCADAData] SENT BY INITIATOR
);
GO

CREATE CONTRACT [//Hartonomous/ModelUpdatedContract]
(
    [//Hartonomous/ModelUpdated] SENT BY INITIATOR
);
GO

PRINT 'Contracts created';
GO

-- =============================================
-- Queues: Message storage with activation
-- =============================================

-- Generic sensor data queue
CREATE QUEUE SensorDataQueue
WITH 
    STATUS = ON,
    RETENTION = OFF,
    ACTIVATION (
        STATUS = ON,
        PROCEDURE_NAME = dbo.sp_ProcessSensorData,
        MAX_QUEUE_READERS = 5,  -- Up to 5 concurrent processors
        EXECUTE AS OWNER
    );
GO

-- Video frame processing queue
CREATE QUEUE VideoFrameQueue
WITH 
    STATUS = ON,
    RETENTION = OFF,
    ACTIVATION (
        STATUS = ON,
        PROCEDURE_NAME = dbo.sp_ProcessVideoFrame,
        MAX_QUEUE_READERS = 10,  -- Video processing is CPU-intensive
        EXECUTE AS OWNER
    );
GO

-- Audio chunk processing queue
CREATE QUEUE AudioChunkQueue
WITH 
    STATUS = ON,
    RETENTION = OFF,
    ACTIVATION (
        STATUS = ON,
        PROCEDURE_NAME = dbo.sp_ProcessAudioChunk,
        MAX_QUEUE_READERS = 5,
        EXECUTE AS OWNER
    );
GO

-- SCADA telemetry queue
CREATE QUEUE SCADADataQueue
WITH 
    STATUS = ON,
    RETENTION = OFF,
    ACTIVATION (
        STATUS = ON,
        PROCEDURE_NAME = dbo.sp_ProcessSCADAData,
        MAX_QUEUE_READERS = 3,
        EXECUTE AS OWNER
    );
GO

-- Model update notification queue
CREATE QUEUE ModelUpdatedQueue
WITH 
    STATUS = ON,
    RETENTION = OFF,
    ACTIVATION (
        STATUS = ON,
        PROCEDURE_NAME = dbo.sp_OnModelUpdated,
        MAX_QUEUE_READERS = 1,  -- Single-threaded for consistency
        EXECUTE AS OWNER
    );
GO

PRINT 'Queues created with activation procedures';
GO

-- =============================================
-- Services: Endpoint for sending/receiving messages
-- =============================================

CREATE SERVICE [//Hartonomous/SensorDataService]
    ON QUEUE SensorDataQueue
    ([//Hartonomous/SensorDataContract]);
GO

CREATE SERVICE [//Hartonomous/VideoFrameService]
    ON QUEUE VideoFrameQueue
    ([//Hartonomous/VideoFrameContract]);
GO

CREATE SERVICE [//Hartonomous/AudioChunkService]
    ON QUEUE AudioChunkQueue
    ([//Hartonomous/AudioChunkContract]);
GO

CREATE SERVICE [//Hartonomous/SCADADataService]
    ON QUEUE SCADADataQueue
    ([//Hartonomous/SCADADataContract]);
GO

CREATE SERVICE [//Hartonomous/ModelUpdatedService]
    ON QUEUE ModelUpdatedQueue
    ([//Hartonomous/ModelUpdatedContract]);
GO

PRINT 'Services created';
GO

-- =============================================
-- Activation Procedures (Placeholders - to be implemented)
-- =============================================

-- Generic sensor data processor
CREATE OR ALTER PROCEDURE dbo.sp_ProcessSensorData
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @conversationHandle UNIQUEIDENTIFIER;
    DECLARE @messageType NVARCHAR(256);
    DECLARE @messageBody XML;
    
    WHILE (1=1)
    BEGIN
        WAITFOR (
            RECEIVE TOP(1)
                @conversationHandle = conversation_handle,
                @messageType = message_type_name,
                @messageBody = CAST(message_body AS XML)
            FROM SensorDataQueue
        ), TIMEOUT 1000;
        
        IF @@ROWCOUNT = 0
            BREAK;
        
        -- Process message (placeholder - actual implementation would generate embeddings)
        PRINT 'Received sensor data message: ' + CAST(@messageBody AS NVARCHAR(MAX));
        
        -- End conversation
        END CONVERSATION @conversationHandle;
    END
END;
GO

-- Video frame processor
CREATE OR ALTER PROCEDURE dbo.sp_ProcessVideoFrame
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @conversationHandle UNIQUEIDENTIFIER;
    DECLARE @messageType NVARCHAR(256);
    DECLARE @messageBody XML;
    
    WHILE (1=1)
    BEGIN
        WAITFOR (
            RECEIVE TOP(1)
                @conversationHandle = conversation_handle,
                @messageType = message_type_name,
                @messageBody = CAST(message_body AS XML)
            FROM VideoFrameQueue
        ), TIMEOUT 1000;
        
        IF @@ROWCOUNT = 0
            BREAK;
        
        -- Extract video frame metadata
        DECLARE @frameId INT = @messageBody.value('(/VideoFrame/FrameId)[1]', 'INT');
        DECLARE @timestamp DATETIME2 = @messageBody.value('(/VideoFrame/Timestamp)[1]', 'DATETIME2');
        
        PRINT 'Processing video frame ' + CAST(@frameId AS NVARCHAR(10)) + ' at ' + CAST(@timestamp AS NVARCHAR(50));
        
        -- TODO: Call C# external activator to generate CLIP embeddings
        -- TODO: INSERT INTO SensorStreams with VECTOR embedding + GEOMETRY spatial projection
        
        END CONVERSATION @conversationHandle;
    END
END;
GO

-- Audio chunk processor
CREATE OR ALTER PROCEDURE dbo.sp_ProcessAudioChunk
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @conversationHandle UNIQUEIDENTIFIER;
    DECLARE @messageType NVARCHAR(256);
    DECLARE @messageBody XML;
    
    WHILE (1=1)
    BEGIN
        WAITFOR (
            RECEIVE TOP(1)
                @conversationHandle = conversation_handle,
                @messageType = message_type_name,
                @messageBody = CAST(message_body AS XML)
            FROM AudioChunkQueue
        ), TIMEOUT 1000;
        
        IF @@ROWCOUNT = 0
            BREAK;
        
        DECLARE @chunkId INT = @messageBody.value('(/AudioChunk/ChunkId)[1]', 'INT');
        DECLARE @durationMs INT = @messageBody.value('(/AudioChunk/DurationMs)[1]', 'INT');
        
        PRINT 'Processing audio chunk ' + CAST(@chunkId AS NVARCHAR(10)) + ' (' + CAST(@durationMs AS NVARCHAR(10)) + 'ms)';
        
        -- TODO: Call Whisper model for transcription
        -- TODO: Generate audio embeddings
        
        END CONVERSATION @conversationHandle;
    END
END;
GO

-- SCADA telemetry processor
CREATE OR ALTER PROCEDURE dbo.sp_ProcessSCADAData
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @conversationHandle UNIQUEIDENTIFIER;
    DECLARE @messageType NVARCHAR(256);
    DECLARE @messageBody XML;
    
    WHILE (1=1)
    BEGIN
        WAITFOR (
            RECEIVE TOP(1)
                @conversationHandle = conversation_handle,
                @messageType = message_type_name,
                @messageBody = CAST(message_body AS XML)
            FROM SCADADataQueue
        ), TIMEOUT 1000;
        
        IF @@ROWCOUNT = 0
            BREAK;
        
        DECLARE @sensorId NVARCHAR(100) = @messageBody.value('(/SCADAData/SensorId)[1]', 'NVARCHAR(100)');
        DECLARE @value FLOAT = @messageBody.value('(/SCADAData/Value)[1]', 'FLOAT');
        
        PRINT 'SCADA data from sensor ' + @sensorId + ': ' + CAST(@value AS NVARCHAR(50));
        
        -- TODO: Generate embedding from sensor telemetry
        -- TODO: Detect anomalies via hybrid search
        
        END CONVERSATION @conversationHandle;
    END
END;
GO

-- Model update notification processor
CREATE OR ALTER PROCEDURE dbo.sp_OnModelUpdated
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @conversationHandle UNIQUEIDENTIFIER;
    DECLARE @messageType NVARCHAR(256);
    DECLARE @messageBody XML;
    
    WHILE (1=1)
    BEGIN
        WAITFOR (
            RECEIVE TOP(1)
                @conversationHandle = conversation_handle,
                @messageType = message_type_name,
                @messageBody = CAST(message_body AS XML)
            FROM ModelUpdatedQueue
        ), TIMEOUT 1000;
        
        IF @@ROWCOUNT = 0
            BREAK;
        
        DECLARE @modelId INT = @messageBody.value('(/ModelUpdated/ModelId)[1]', 'INT');
        
        PRINT 'Model ' + CAST(@modelId AS NVARCHAR(10)) + ' updated - invalidating cached inferences';
        
        -- TODO: Invalidate cached inferences for this model
        -- TODO: Trigger re-computation of downstream embeddings
        
        END CONVERSATION @conversationHandle;
    END
END;
GO

PRINT 'Activation procedures created (placeholders - implement actual logic)';
GO

-- =============================================
-- Helper: Send Message to Queue
-- =============================================

CREATE OR ALTER PROCEDURE dbo.sp_SendMessage
    @serviceName NVARCHAR(256),
    @contractName NVARCHAR(256),
    @messageType NVARCHAR(256),
    @messageBody XML
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @conversationHandle UNIQUEIDENTIFIER;
    
    BEGIN TRANSACTION;
    
    -- Begin conversation
    BEGIN DIALOG CONVERSATION @conversationHandle
        FROM SERVICE [//Hartonomous/SensorDataService]  -- Initiator service (placeholder)
        TO SERVICE @serviceName
        ON CONTRACT @contractName
        WITH ENCRYPTION = OFF;
    
    -- Send message
    SEND ON CONVERSATION @conversationHandle
        MESSAGE TYPE @messageType
        (@messageBody);
    
    COMMIT TRANSACTION;
    
    PRINT 'Message sent on conversation ' + CAST(@conversationHandle AS NVARCHAR(100));
END;
GO

-- =============================================
-- Example Usage: Send Test Messages
-- =============================================
/*
-- Send video frame message
DECLARE @videoFrameXML XML = '
<VideoFrame>
    <FrameId>1</FrameId>
    <Timestamp>2025-10-27T12:00:00</Timestamp>
    <Source>Camera1</Source>
    <Resolution>1920x1080</Resolution>
</VideoFrame>';

EXEC dbo.sp_SendMessage 
    @serviceName = N'//Hartonomous/VideoFrameService',
    @contractName = N'//Hartonomous/VideoFrameContract',
    @messageType = N'//Hartonomous/VideoFrame',
    @messageBody = @videoFrameXML;

-- Send SCADA data message
DECLARE @scadaXML XML = '
<SCADAData>
    <SensorId>HVAC_Temp_Zone1</SensorId>
    <Value>72.5</Value>
    <Unit>Fahrenheit</Unit>
    <Timestamp>2025-10-27T12:00:00</Timestamp>
</SCADAData>';

EXEC dbo.sp_SendMessage 
    @serviceName = N'//Hartonomous/SCADADataService',
    @contractName = N'//Hartonomous/SCADADataContract',
    @messageType = N'//Hartonomous/SCADAData',
    @messageBody = @scadaXML;

-- Check queue status
SELECT * FROM SensorDataQueue;
SELECT * FROM VideoFrameQueue;
SELECT * FROM AudioChunkQueue;
SELECT * FROM SCADADataQueue;
*/
GO

PRINT '==============================================';
PRINT 'Service Broker setup complete!';
PRINT 'Event-driven architecture ready for:';
PRINT '  - Video frame processing';
PRINT '  - Audio chunk processing';
PRINT '  - SCADA telemetry processing';
PRINT '  - Model update notifications';
PRINT 'Next: Implement external activators in C# for embedding generation';
PRINT '==============================================';
GO

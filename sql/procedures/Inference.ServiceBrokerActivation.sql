-- =============================================
-- Service Broker Event-Driven Job Processing
-- =============================================
-- Replaces polling-based InferenceRequests with native SQL Server queues
-- This is the "all-in-SQL, no-polling" architecture
-- =============================================

USE Hartonomous;
GO

-- Create message types for inference jobs
IF NOT EXISTS (SELECT 1 FROM sys.service_message_types WHERE name = 'InferenceJobRequest')
BEGIN
    CREATE MESSAGE TYPE [InferenceJobRequest]
    VALIDATION = WELL_FORMED_XML;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.service_message_types WHERE name = 'InferenceJobResponse')
BEGIN
    CREATE MESSAGE TYPE [InferenceJobResponse]
    VALIDATION = WELL_FORMED_XML;
END;
GO

-- Create contract for inference jobs
IF NOT EXISTS (SELECT 1 FROM sys.service_contracts WHERE name = 'InferenceJobContract')
BEGIN
    CREATE CONTRACT [InferenceJobContract]
    (
        [InferenceJobRequest] SENT BY INITIATOR,
        [InferenceJobResponse] SENT BY TARGET
    );
END;
GO

-- Create queue for inference jobs
IF NOT EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'InferenceQueue')
BEGIN
    CREATE QUEUE [InferenceQueue]
    WITH STATUS = ON,
         RETENTION = OFF;
END;
GO

-- Create service for inference jobs
IF EXISTS (SELECT 1 FROM sys.services WHERE name = 'InferenceService')
    DROP SERVICE [InferenceService];
GO

CREATE SERVICE [InferenceService]
ON QUEUE [InferenceQueue]
([InferenceJobContract]);
GO

-- =============================================
-- ACTIVATED PROCEDURE: Execute Inference
-- =============================================
-- This procedure is automatically activated when a message arrives
-- It replaces the C# InferenceJobWorker polling service
-- =============================================

CREATE OR ALTER PROCEDURE dbo.sp_ExecuteInference_Activated
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @conversation_handle UNIQUEIDENTIFIER;
    DECLARE @message_body NVARCHAR(MAX);
    DECLARE @message_type_name NVARCHAR(256);
    
    -- Process messages in a loop until queue is empty
    WHILE (1=1)
    BEGIN
        BEGIN TRANSACTION;
        
        -- Receive message from queue
        WAITFOR (
            RECEIVE TOP(1)
                @conversation_handle = conversation_handle,
                @message_body = CAST(message_body AS NVARCHAR(MAX)),
                @message_type_name = message_type_name
            FROM InferenceQueue
        ), TIMEOUT 1000; -- 1 second timeout
        
        -- If no message, exit
        IF @@ROWCOUNT = 0
        BEGIN
            COMMIT TRANSACTION;
            BREAK;
        END
        
        -- Process the message based on type
        IF @message_type_name = 'InferenceJobRequest'
        BEGIN
            BEGIN TRY
                -- Parse the job request XML
                DECLARE @JobXml XML = CAST(@message_body AS XML);
                DECLARE @InferenceId BIGINT = @JobXml.value('(/InferenceJob/InferenceId)[1]', 'BIGINT');
                DECLARE @TaskType NVARCHAR(50) = @JobXml.value('(/InferenceJob/TaskType)[1]', 'NVARCHAR(50)');
                DECLARE @InputData NVARCHAR(MAX) = @JobXml.value('(/InferenceJob/InputData)[1]', 'NVARCHAR(MAX)');
                DECLARE @ModelId INT = @JobXml.value('(/InferenceJob/ModelId)[1]', 'INT');
                DECLARE @TenantId INT = @JobXml.value('(/InferenceJob/TenantId)[1]', 'INT');
                
                -- Execute the inference job
                DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
                DECLARE @OutputData NVARCHAR(MAX);
                DECLARE @Confidence DECIMAL(5,4);
                DECLARE @GenerationStreamId BIGINT;
                
                -- Call the appropriate inference procedure based on task type
                IF @TaskType = 'text-generation'
                BEGIN
                    -- Use attention-based generation
                    DECLARE @InputAtomIds NVARCHAR(MAX) = JSON_VALUE(@InputData, '$.inputAtomIds');
                    DECLARE @MaxTokens INT = JSON_VALUE(@InputData, '$.maxTokens');
                    DECLARE @Temperature FLOAT = JSON_VALUE(@InputData, '$.temperature');
                    DECLARE @TopK INT = JSON_VALUE(@InputData, '$.topK');
                    DECLARE @TopP FLOAT = JSON_VALUE(@InputData, '$.topP');
                    
                    SET @GenerationStreamId = dbo.fn_GenerateWithAttention(
                        @ModelId,
                        @InputAtomIds,
                        @InputData,
                        @MaxTokens,
                        @Temperature,
                        @TopK,
                        @TopP,
                        8, -- attention heads
                        @TenantId
                    );
                    
                    -- Retrieve the generated output
                    SELECT @OutputData = JSON_OBJECT(
                        'generationStreamId': @GenerationStreamId,
                        'status': 'completed'
                    );
                    SET @Confidence = 0.95;
                END
                ELSE IF @TaskType = 'image-generation'
                BEGIN
                    -- Call image generation procedure
                    EXEC dbo.sp_GenerateImage 
                        @Prompt = @InputData,
                        @ModelId = @ModelId,
                        @TenantId = @TenantId,
                        @OutputAtomId = @GenerationStreamId OUTPUT;
                    
                    SET @OutputData = JSON_OBJECT('atomId': @GenerationStreamId);
                    SET @Confidence = 0.90;
                END
                ELSE IF @TaskType = 'audio-generation'
                BEGIN
                    -- Call audio generation procedure
                    EXEC dbo.sp_GenerateAudio
                        @Prompt = @InputData,
                        @ModelId = @ModelId,
                        @TenantId = @TenantId,
                        @OutputAtomId = @GenerationStreamId OUTPUT;
                    
                    SET @OutputData = JSON_OBJECT('atomId': @GenerationStreamId);
                    SET @Confidence = 0.88;
                END
                ELSE
                BEGIN
                    -- Unknown task type
                    SET @OutputData = JSON_OBJECT('error': 'Unknown task type: ' + @TaskType);
                    SET @Confidence = 0.0;
                END
                
                DECLARE @DurationMs INT = DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME());
                
                -- Update the inference request with results
                UPDATE dbo.InferenceRequests
                SET Status = 'Completed',
                    OutputData = @OutputData,
                    Confidence = @Confidence,
                    TotalDurationMs = @DurationMs,
                    CompletionTimestamp = SYSUTCDATETIME()
                WHERE InferenceId = @InferenceId;
                
                -- Send response back
                DECLARE @ResponseXml XML = (
                    SELECT @InferenceId AS InferenceId,
                           'Completed' AS Status,
                           @OutputData AS OutputData,
                           @Confidence AS Confidence,
                           @DurationMs AS DurationMs
                    FOR XML PATH('InferenceResponse'), TYPE
                );
                
                SEND ON CONVERSATION @conversation_handle
                    MESSAGE TYPE [InferenceJobResponse]
                    (@ResponseXml);
                
                -- End the conversation
                END CONVERSATION @conversation_handle;
                
            END TRY
            BEGIN CATCH
                -- Handle errors
                DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
                
                UPDATE dbo.InferenceRequests
                SET Status = 'Failed',
                    OutputData = JSON_OBJECT('error': @ErrorMessage),
                    CompletionTimestamp = SYSUTCDATETIME()
                WHERE InferenceId = @InferenceId;
                
                -- Send error response
                DECLARE @ErrorXml XML = (
                    SELECT @InferenceId AS InferenceId,
                           'Failed' AS Status,
                           @ErrorMessage AS Error
                    FOR XML PATH('InferenceError'), TYPE
                );
                
                SEND ON CONVERSATION @conversation_handle
                    MESSAGE TYPE [InferenceJobResponse]
                    (@ErrorXml);
                
                END CONVERSATION @conversation_handle;
            END CATCH
        END
        ELSE IF @message_type_name = 'http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog'
        BEGIN
            -- End dialog acknowledgment
            END CONVERSATION @conversation_handle;
        END
        ELSE IF @message_type_name = 'http://schemas.microsoft.com/SQL/ServiceBroker/Error'
        BEGIN
            -- Handle broker error
            END CONVERSATION @conversation_handle;
        END;
        
        COMMIT TRANSACTION;
    END
END;
GO

-- =============================================
-- Enable queue activation
-- =============================================
ALTER QUEUE [InferenceQueue]
WITH ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = dbo.sp_ExecuteInference_Activated,
    MAX_QUEUE_READERS = 5, -- Parallel processing with up to 5 concurrent activations
    EXECUTE AS OWNER
);
GO

PRINT 'Service Broker inference queue activated successfully';
PRINT 'Queue: InferenceQueue';
PRINT 'Activation Procedure: sp_ExecuteInference_Activated';
PRINT 'Max Concurrent Readers: 5';
GO

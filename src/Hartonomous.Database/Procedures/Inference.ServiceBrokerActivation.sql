-- =============================================
-- Service Broker Event-Driven Job Processing
-- =============================================
-- Replaces polling-based InferenceRequests with native SQL Server queues
-- This is the "all-in-SQL, no-polling" architecture
-- =============================================

-- Create message types for inference jobs
IF NOT EXISTS (SELECT 1 FROM sys.service_message_types WHERE name = 'InferenceJobRequest')
BEGIN
    CREATE MESSAGE TYPE [InferenceJobRequest]
    VALIDATION = WELL_FORMED_XML;
END;

IF NOT EXISTS (SELECT 1 FROM sys.service_message_types WHERE name = 'InferenceJobResponse')
BEGIN
    CREATE MESSAGE TYPE [InferenceJobResponse]
    VALIDATION = WELL_FORMED_XML;
END;

-- Create contract for inference jobs
IF NOT EXISTS (SELECT 1 FROM sys.service_contracts WHERE name = 'InferenceJobContract')
BEGIN
    CREATE CONTRACT [InferenceJobContract]
    (
        [InferenceJobRequest] SENT BY INITIATOR,
        [InferenceJobResponse] SENT BY TARGET
    );
END;

-- Create queue for inference jobs
IF NOT EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'InferenceQueue')
BEGIN
    CREATE QUEUE [InferenceQueue]
    WITH STATUS = ON,
         RETENTION = OFF;
END;

-- Create service for inference jobs
IF EXISTS (SELECT 1 FROM sys.services WHERE name = 'InferenceService')
    DROP SERVICE [InferenceService];

CREATE SERVICE [InferenceService]
ON QUEUE [InferenceQueue]
([InferenceJobContract]);

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






                -- Execute the inference job




                -- Call the appropriate inference procedure based on task type
                IF @TaskType = 'text-generation'
                BEGIN
                    -- Use attention-based generation





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

                -- Update the inference request with results
                UPDATE dbo.InferenceRequests
                SET Status = 'Completed',
                    OutputData = @OutputData,
                    Confidence = @Confidence,
                    TotalDurationMs = @DurationMs,
                    CompletionTimestamp = SYSUTCDATETIME()
                WHERE InferenceId = @InferenceId;
                
                -- Send response back

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

                UPDATE dbo.InferenceRequests
                SET Status = 'Failed',
                    OutputData = JSON_OBJECT('error': @ErrorMessage),
                    CompletionTimestamp = SYSUTCDATETIME()
                WHERE InferenceId = @InferenceId;
                
                -- Send error response

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

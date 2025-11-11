CREATE PROCEDURE dbo.sp_ExecuteInference_Activated
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @conversation_handle UNIQUEIDENTIFIER;
    DECLARE @message_body NVARCHAR(MAX);
    DECLARE @message_type_name NVARCHAR(256);
    
    WHILE (1=1)
    BEGIN
        BEGIN TRANSACTION;
        
        WAITFOR (
            RECEIVE TOP(1)
                @conversation_handle = conversation_handle,
                @message_body = CAST(message_body AS NVARCHAR(MAX)),
                @message_type_name = message_type_name
            FROM InferenceQueue
        ), TIMEOUT 1000;
        
        IF @@ROWCOUNT = 0
        BEGIN
            COMMIT TRANSACTION;
            BREAK;
        END
        
        IF @message_type_name = 'InferenceJobRequest'
        BEGIN
            BEGIN TRY
                DECLARE @JobXml XML = CAST(@message_body AS XML);
                DECLARE @InferenceId BIGINT = @JobXml.value('(/InferenceJob/InferenceId)[1]', 'BIGINT');
                DECLARE @TaskType NVARCHAR(50) = @JobXml.value('(/InferenceJob/TaskType)[1]', 'NVARCHAR(50)');
                DECLARE @InputData NVARCHAR(MAX) = @JobXml.value('(/InferenceJob/InputData)[1]', 'NVARCHAR(MAX)');
                DECLARE @ModelId INT = @JobXml.value('(/InferenceJob/ModelId)[1]', 'INT');
                DECLARE @TenantId INT = @JobXml.value('(/InferenceJob/TenantId)[1]', 'INT');
                
                DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
                DECLARE @OutputData NVARCHAR(MAX);
                DECLARE @Confidence DECIMAL(5,4);
                DECLARE @GenerationStreamId BIGINT;
                
                IF @TaskType = 'text-generation'
                BEGIN
                    DECLARE @InputAtomIds NVARCHAR(MAX) = JSON_VALUE(@InputData, '$.inputAtomIds');
                    DECLARE @MaxTokens INT = JSON_VALUE(@InputData, '$.maxTokens');
                    DECLARE @Temperature FLOAT = JSON_VALUE(@InputData, '$.temperature');
                    DECLARE @TopK INT = JSON_VALUE(@InputData, '$.topK');
                    DECLARE @TopP FLOAT = JSON_VALUE(@InputData, '$.topP');
                    
                    --SET @GenerationStreamId = dbo.fn_GenerateWithAttention(
                    --    @ModelId,
                    --    @InputAtomIds,
                    --    @InputData,
                    --    @MaxTokens,
                    --    @Temperature,
                    --    @TopK,
                    --    @TopP,
                    --    8, -- attention heads
                    --    @TenantId
                    --);
                    
                    SELECT @OutputData = (SELECT @GenerationStreamId as generationStreamId, 'completed' as status FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
                    SET @Confidence = 0.95;
                END
                ELSE IF @TaskType = 'image-generation'
                BEGIN
                    EXEC dbo.sp_GenerateImage 
                        @Prompt = @InputData,
                        @ModelId = @ModelId,
                        @TenantId = @TenantId,
                        @OutputAtomId = @GenerationStreamId OUTPUT;
                    
                    SELECT @OutputData = (SELECT @GenerationStreamId as atomId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
                    SET @Confidence = 0.90;
                END
                ELSE IF @TaskType = 'audio-generation'
                BEGIN
                    EXEC dbo.sp_GenerateAudio
                        @Prompt = @InputData,
                        @ModelId = @ModelId,
                        @TenantId = @TenantId,
                        @OutputAtomId = @GenerationStreamId OUTPUT;
                    
                    SELECT @OutputData = (SELECT @GenerationStreamId as atomId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
                    SET @Confidence = 0.88;
                END
                ELSE
                BEGIN
                    SELECT @OutputData = (SELECT 'Unknown task type: ' + @TaskType as error FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
                    SET @Confidence = 0.0;
                END
                
                DECLARE @DurationMs INT = DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME());
                
                UPDATE dbo.InferenceRequests
                SET Status = 'Completed',
                    OutputData = @OutputData,
                    Confidence = @Confidence,
                    TotalDurationMs = @DurationMs,
                    CompletionTimestamp = SYSUTCDATETIME()
                WHERE InferenceId = @InferenceId;
                
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
                
                END CONVERSATION @conversation_handle;
                
            END TRY
            BEGIN CATCH
                DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
                
                UPDATE dbo.InferenceRequests
                SET Status = 'Failed',
                    OutputData = (SELECT @ErrorMessage as error FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
                    CompletionTimestamp = SYSUTCDATETIME()
                WHERE InferenceId = @InferenceId;
                
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
            END CONVERSATION @conversation_handle;
        END
        ELSE IF @message_type_name = 'http://schemas.microsoft.com/SQL/ServiceBroker/Error'
        BEGIN
            END CONVERSATION @conversation_handle;
        END;
        
        COMMIT TRANSACTION;
    END
END;
GO
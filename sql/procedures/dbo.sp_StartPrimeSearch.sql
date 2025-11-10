USE Hartonomous;
GO

CREATE OR ALTER PROCEDURE dbo.sp_StartPrimeSearch
    @RangeStart BIGINT,
    @RangeEnd BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    -- Validate the input range
    IF @RangeStart IS NULL OR @RangeEnd IS NULL OR @RangeStart < 2 OR @RangeEnd <= @RangeStart
    BEGIN
        RAISERROR('Invalid range specified. @RangeStart must be >= 2 and less than @RangeEnd.', 16, 1);
        RETURN;
    END

    -- Create the initial message payload for the Analyze phase.
    -- This message contains the full scope of the long-running job.
    DECLARE @JobPayload NVARCHAR(MAX) = JSON_OBJECT(
        'jobType': 'LongRunningPrimeSearch',
        'fullRangeStart': @RangeStart,
        'fullRangeEnd': @RangeEnd,
        'nextChunkStart': @RangeStart, -- The first chunk to be processed
        'chunkSize': 10000, -- Define a manageable chunk size
        'primesFound': JSON_QUERY('[]')
    );

    DECLARE @MessageBody XML = CAST(@JobPayload AS XML);
    DECLARE @ConversationHandle UNIQUEIDENTIFIER;

    -- Begin a dialog and send the initial message to the AnalyzeQueue.
    -- The OODA loop will pick this up and start the process.
    BEGIN DIALOG CONVERSATION @ConversationHandle
        FROM SERVICE [//Hartonomous/Service/Initiator] -- Assuming a generic initiator service
        TO SERVICE '//Hartonomous/Service/Analyze'
        ON CONTRACT [//Hartonomous/Contract/OODA]
        WITH ENCRYPTION = OFF;

    SEND ON CONVERSATION @ConversationHandle
        MESSAGE TYPE [//Hartonomous/Message/Analyze]
        (@MessageBody);

    PRINT 'Prime number search job initiated for range ' + CAST(@RangeStart AS NVARCHAR(20)) + ' to ' + CAST(@RangeEnd AS NVARCHAR(20)) + '.';
    PRINT 'The autonomous OODA loop will now process this job in chunks.';

END;
GO

PRINT 'Created procedure dbo.sp_StartPrimeSearch.';
GO

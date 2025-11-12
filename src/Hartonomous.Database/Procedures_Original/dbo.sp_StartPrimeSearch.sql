USE Hartonomous;
GO

-- sp_StartPrimeSearch: Gödel Engine "Ignition Key"
-- Creates a compute job in AutonomousComputeJobs table
-- Sends initial message to AnalyzeQueue to trigger the OODA loop
-- The autonomous loop will process the job incrementally using Service Broker

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

    -- Create the job in the tracking table
    DECLARE @JobId UNIQUEIDENTIFIER = NEWID();
    
    INSERT INTO dbo.AutonomousComputeJobs (JobId, JobType, Status, JobParameters, CurrentState)
    VALUES (
        @JobId,
        'PrimeSearch',
        'Running',
        JSON_OBJECT('rangeStart': @RangeStart, 'rangeEnd': @RangeEnd),
        JSON_OBJECT('lastChecked': @RangeStart - 1)
    );

    -- Send the initial "wake up" message to the AnalyzeQueue
    DECLARE @MessageBody XML = (
        SELECT @JobId AS JobId
        FOR XML PATH('JobRequest'), ROOT('Analysis')
    );
    
    DECLARE @ConversationHandle UNIQUEIDENTIFIER;

    BEGIN DIALOG CONVERSATION @ConversationHandle
        FROM SERVICE [//Hartonomous/Service/Initiator]
        TO SERVICE 'AnalyzeService'
        ON CONTRACT [//Hartonomous/AutonomousLoop/AnalyzeContract]
        WITH ENCRYPTION = OFF;

    SEND ON CONVERSATION @ConversationHandle
        MESSAGE TYPE [//Hartonomous/AutonomousLoop/AnalyzeMessage]
        (@MessageBody);

    PRINT 'Prime number search job initiated with JobId: ' + CAST(@JobId AS NVARCHAR(36));
    PRINT 'Range: [' + CAST(@RangeStart AS NVARCHAR(20)) + ', ' + CAST(@RangeEnd AS NVARCHAR(20)) + ']';
    PRINT 'The autonomous OODA loop will now process this job in chunks.';

END;
GO

PRINT 'Created procedure dbo.sp_StartPrimeSearch.';
GO

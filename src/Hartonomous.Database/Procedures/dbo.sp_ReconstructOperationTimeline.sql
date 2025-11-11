CREATE PROCEDURE dbo.sp_ReconstructOperationTimeline
    @OperationId UNIQUEIDENTIFIER,
    @IncludePayloads BIT = 0,
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @Debug = 1
        PRINT 'Reconstructing operation timeline for ' + CAST(@OperationId AS NVARCHAR(36));

    -- TODO: Fix AtomicStream UDT reference
    /*
    DECLARE @ProvenanceStream NVARCHAR(MAX);
    SELECT @ProvenanceStream = ProvenanceStream
    FROM dbo.OperationProvenance
    WHERE OperationId = @OperationId;

    IF @ProvenanceStream IS NULL
    BEGIN
        RAISERROR('No provenance stream found for operation %s', 16, 1, @OperationId);
        RETURN;
    END

    DECLARE @Stream AtomicStream = AtomicStream::Parse(@ProvenanceStream);
    DECLARE @Timeline TABLE (
        SequenceNumber INT,
        Timestamp DATETIME2,
        SegmentKind NVARCHAR(50),
        ContentType NVARCHAR(100),
        Metadata NVARCHAR(MAX),
        PayloadSize INT
    );

    DECLARE @SegmentIndex INT = 0;
    DECLARE @SegmentCount INT = @Stream.SegmentCount;

    WHILE @SegmentIndex < @SegmentCount
    BEGIN
        INSERT INTO @Timeline
        SELECT
            @SegmentIndex,
            @Stream.GetSegmentTimestamp(@SegmentIndex),
            @Stream.GetSegmentKind(@SegmentIndex),
            @Stream.GetSegmentContentType(@SegmentIndex),
            @Stream.GetSegmentMetadata(@SegmentIndex),
            DATALENGTH(@Stream.GetSegmentPayload(@SegmentIndex));

        SET @SegmentIndex = @SegmentIndex + 1;
    END

    IF @IncludePayloads = 1
    BEGIN
        DECLARE @FullTimeline TABLE (
            SequenceNumber INT,
            Timestamp DATETIME2,
            SegmentKind NVARCHAR(50),
            ContentType NVARCHAR(100),
            Metadata NVARCHAR(MAX),
            Payload VARBINARY(MAX)
        );

        SET @SegmentIndex = 0;
        WHILE @SegmentIndex < @SegmentCount
        BEGIN
            INSERT INTO @FullTimeline
            SELECT
                @SegmentIndex,
                @Stream.GetSegmentTimestamp(@SegmentIndex),
                @Stream.GetSegmentKind(@SegmentIndex),
                @Stream.GetSegmentContentType(@SegmentIndex),
                @Stream.GetSegmentMetadata(@SegmentIndex),
                @Stream.GetSegmentPayload(@SegmentIndex);

            SET @SegmentIndex = @SegmentIndex + 1;
        END

        SELECT * FROM @FullTimeline ORDER BY SequenceNumber;
    END
    ELSE
    BEGIN
        SELECT * FROM @Timeline ORDER BY SequenceNumber;
    END
    */

    IF @Debug = 1
        PRINT 'Operation timeline reconstructed';
END;
GO
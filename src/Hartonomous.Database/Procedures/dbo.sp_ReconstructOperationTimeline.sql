CREATE PROCEDURE dbo.sp_ReconstructOperationTimeline
    @OperationId UNIQUEIDENTIFIER,
    @IncludePayloads BIT = 0,
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @Debug = 1
        PRINT 'Reconstructing operation timeline for ' + CAST(@OperationId AS NVARCHAR(36));

    DECLARE @ProvenanceStream provenance.AtomicStream;
    SELECT @ProvenanceStream = ProvenanceStream
    FROM dbo.OperationProvenance
    WHERE OperationId = @OperationId;

    IF @ProvenanceStream IS NULL OR @ProvenanceStream.IsNull = 1
    BEGIN
        RAISERROR('No provenance stream found for operation %s', 16, 1, @OperationId);
        RETURN;
    END

    DECLARE @Timeline TABLE (
        SequenceNumber INT,
        Timestamp DATETIME2,
        SegmentKind NVARCHAR(50),
        ContentType NVARCHAR(100),
        Metadata NVARCHAR(MAX),
        PayloadSize INT
    );

    DECLARE @SegmentIndex INT = 0;
    DECLARE @SegmentCount INT = @ProvenanceStream.SegmentCount;

    WHILE @SegmentIndex < @SegmentCount
    BEGIN
        INSERT INTO @Timeline
        SELECT
            @SegmentIndex,
            @ProvenanceStream.GetSegmentTimestamp(@SegmentIndex),
            @ProvenanceStream.GetSegmentKind(@SegmentIndex),
            @ProvenanceStream.GetSegmentContentType(@SegmentIndex),
            @ProvenanceStream.GetSegmentMetadata(@SegmentIndex),
            DATALENGTH(@ProvenanceStream.GetSegmentPayload(@SegmentIndex));

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
                @ProvenanceStream.GetSegmentTimestamp(@SegmentIndex),
                @ProvenanceStream.GetSegmentKind(@SegmentIndex),
                @ProvenanceStream.GetSegmentContentType(@SegmentIndex),
                @ProvenanceStream.GetSegmentMetadata(@SegmentIndex),
                @ProvenanceStream.GetSegmentPayload(@SegmentIndex);

            SET @SegmentIndex = @SegmentIndex + 1;
        END

        SELECT * FROM @FullTimeline ORDER BY SequenceNumber;
    END
    ELSE
    BEGIN
        SELECT * FROM @Timeline ORDER BY SequenceNumber;
    END

    IF @Debug = 1
        PRINT 'Operation timeline reconstructed';
END;
GO
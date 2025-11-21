CREATE OR ALTER PROCEDURE dbo.sp_ValidateOperationProvenance
    @OperationId UNIQUEIDENTIFIER,
    @ExpectedScope NVARCHAR(100) = NULL,
    @ExpectedModel NVARCHAR(100) = NULL,
    @MinSegments INT = 1,
    @MaxAgeHours INT = 24,
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
    DECLARE @ValidationResult TABLE (
        CheckName NVARCHAR(100),
        Status NVARCHAR(20), -- 'PASS', 'FAIL', 'WARN'
        Details NVARCHAR(MAX)
    );

    IF @Debug = 1
        PRINT 'Validating operation provenance for ' + CAST(@OperationId AS NVARCHAR(36));

    -- Get operation provenance stream
    DECLARE @ProvenanceStream dbo.AtomicStream;
    SELECT @ProvenanceStream = ProvenanceStream
    FROM dbo.OperationProvenance
    WHERE OperationId = @OperationId;

    IF @ProvenanceStream IS NULL OR @ProvenanceStream.IsNull = 1
    BEGIN
        INSERT INTO @ValidationResult VALUES ('Stream Existence', 'FAIL', 'No provenance stream found for operation');
        GOTO ValidationComplete;
    END

    -- Validate stream metadata
    INSERT INTO @ValidationResult VALUES ('Stream Existence', 'PASS', 'Provenance stream exists');

    DECLARE @Scope NVARCHAR(128) = @ProvenanceStream.Scope;
    DECLARE @Model NVARCHAR(128) = @ProvenanceStream.Model;

    -- Check scope
    IF @ExpectedScope IS NOT NULL
    BEGIN
        IF @Scope = @ExpectedScope
            INSERT INTO @ValidationResult VALUES ('Scope Validation', 'PASS', 'Scope matches expected value: ' + @ExpectedScope);
        ELSE
            INSERT INTO @ValidationResult VALUES ('Scope Validation', 'FAIL', 'Scope mismatch. Expected: ' + @ExpectedScope + ', Actual: ' + ISNULL(@Scope, N'<NULL>'));
    END

    -- Check model
    IF @ExpectedModel IS NOT NULL
    BEGIN
        IF @Model = @ExpectedModel
            INSERT INTO @ValidationResult VALUES ('Model Validation', 'PASS', 'Model matches expected value: ' + @ExpectedModel);
        ELSE
            INSERT INTO @ValidationResult VALUES ('Model Validation', 'FAIL', 'Model mismatch. Expected: ' + @ExpectedModel + ', Actual: ' + ISNULL(@Model, N'<NULL>'));
    END

    -- Check segment count
    DECLARE @SegmentCount INT = @ProvenanceStream.SegmentCount;
    IF @SegmentCount >= @MinSegments
        INSERT INTO @ValidationResult VALUES ('Segment Count', 'PASS', 'Segment count (' + CAST(@SegmentCount AS NVARCHAR(10)) + ') meets minimum requirement (' + CAST(@MinSegments AS NVARCHAR(10)) + ')');
    ELSE
        INSERT INTO @ValidationResult VALUES ('Segment Count', 'FAIL', 'Segment count (' + CAST(@SegmentCount AS NVARCHAR(10)) + ') below minimum requirement (' + CAST(@MinSegments AS NVARCHAR(10)) + ')');

    -- Check stream age
    DECLARE @StreamCreatedUtc DATETIME2 = @ProvenanceStream.CreatedUtc;
    IF @StreamCreatedUtc IS NOT NULL
    BEGIN
        DECLARE @StreamAgeHours FLOAT = DATEDIFF(MINUTE, @StreamCreatedUtc, SYSUTCDATETIME()) / 60.0;
        IF @StreamAgeHours <= @MaxAgeHours
            INSERT INTO @ValidationResult VALUES ('Stream Age', 'PASS', 'Stream age (' + CAST(@StreamAgeHours AS NVARCHAR(10)) + ' hours) within limit (' + CAST(@MaxAgeHours AS NVARCHAR(10)) + ' hours)');
        ELSE
            INSERT INTO @ValidationResult VALUES ('Stream Age', 'WARN', 'Stream age (' + CAST(@StreamAgeHours AS NVARCHAR(10)) + ' hours) exceeds limit (' + CAST(@MaxAgeHours AS NVARCHAR(10)) + ' hours)');
    END
    ELSE
    BEGIN
        INSERT INTO @ValidationResult VALUES ('Stream Age', 'WARN', 'Provenance stream creation timestamp is missing');
    END

    -- Validate segment sequence and content
    DECLARE @SegmentIndex INT = 0;
    WHILE @SegmentIndex < @SegmentCount
    BEGIN
        DECLARE @SegmentKind NVARCHAR(50) = @ProvenanceStream.GetSegmentKind(@SegmentIndex);
        DECLARE @SegmentTimestamp DATETIME2 = @ProvenanceStream.GetSegmentTimestamp(@SegmentIndex);

        -- Check for required segment types
        IF @SegmentKind = 'Input' AND @SegmentIndex = 0
            INSERT INTO @ValidationResult VALUES ('Input Segment', 'PASS', 'Valid input segment at position 0');
        ELSE IF @SegmentKind = 'Output' AND @SegmentIndex > 0
            INSERT INTO @ValidationResult VALUES ('Output Segment', 'PASS', 'Valid output segment at position ' + CAST(@SegmentIndex AS NVARCHAR(10)));
        ELSE IF @SegmentKind NOT IN ('Input', 'Output', 'Embedding', 'Moderation', 'Artifact', 'Telemetry', 'Control')
            INSERT INTO @ValidationResult VALUES ('Segment Kind', 'WARN', 'Unknown segment kind: ' + ISNULL(@SegmentKind, N'<NULL>') + ' at position ' + CAST(@SegmentIndex AS NVARCHAR(10)));

        -- Check timestamp ordering
        IF @SegmentIndex > 0
        BEGIN
            DECLARE @PrevTimestamp DATETIME2 = @ProvenanceStream.GetSegmentTimestamp(@SegmentIndex - 1);
            IF @SegmentTimestamp IS NULL OR @PrevTimestamp IS NULL OR @SegmentTimestamp >= @PrevTimestamp
                INSERT INTO @ValidationResult VALUES ('Timestamp Ordering', 'PASS', 'Timestamps properly ordered at position ' + CAST(@SegmentIndex AS NVARCHAR(10)));
            ELSE
                INSERT INTO @ValidationResult VALUES ('Timestamp Ordering', 'FAIL', 'Timestamp out of order at position ' + CAST(@SegmentIndex AS NVARCHAR(10)));
        END

        SET @SegmentIndex = @SegmentIndex + 1;
    END

    ValidationComplete:
    -- Store validation result
    INSERT INTO dbo.ProvenanceValidationResults (
        OperationId,
        ValidationResults,
        OverallStatus,
        ValidationDurationMs,
        ValidatedAt
    )
    VALUES (
        @OperationId,
        (SELECT * FROM @ValidationResult FOR JSON PATH),
        CASE WHEN EXISTS (SELECT 1 FROM @ValidationResult WHERE Status = 'FAIL') THEN 'FAIL'
             WHEN EXISTS (SELECT 1 FROM @ValidationResult WHERE Status = 'WARN') THEN 'WARN'
             ELSE 'PASS' END,
        DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()),
        SYSUTCDATETIME()
    );

    -- Return validation results
    SELECT
        @OperationId AS OperationId,
        CheckName,
        Status,
        Details
    FROM @ValidationResult
    ORDER BY CASE Status WHEN 'FAIL' THEN 1 WHEN 'WARN' THEN 2 WHEN 'PASS' THEN 3 END, CheckName;

    IF @Debug = 1
        PRINT 'Provenance validation completed';
END;
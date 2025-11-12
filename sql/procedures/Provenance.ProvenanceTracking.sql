-- Provenance Tracking Procedures
-- Uses AtomicStream UDT for nano-scale operation logging and audit trails
-- Validates autonomous system operation provenance

-- sp_ValidateOperationProvenance: Validate operation provenance chain
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
    DECLARE @ProvenanceStream provenance.AtomicStream;
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
GO

-- sp_AuditProvenanceChain: Comprehensive audit of provenance chains
CREATE OR ALTER PROCEDURE dbo.sp_AuditProvenanceChain
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL,
    @Scope NVARCHAR(100) = NULL,
    @MinValidationScore FLOAT = 0.8,
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();

    IF @StartDate IS NULL SET @StartDate = DATEADD(DAY, -7, SYSUTCDATETIME());
    IF @EndDate IS NULL SET @EndDate = SYSUTCDATETIME();

    IF @Debug = 1
        PRINT 'Auditing provenance chains from ' + CAST(@StartDate AS NVARCHAR(30)) + ' to ' + CAST(@EndDate AS NVARCHAR(30));

    -- Get operations in date range
    DECLARE @Operations TABLE (
        OperationId UNIQUEIDENTIFIER,
        Scope NVARCHAR(100),
        Model NVARCHAR(100),
        CreatedAt DATETIME2,
        SegmentCount INT,
        ValidationStatus NVARCHAR(20),
        ValidationScore FLOAT
    );

    INSERT INTO @Operations
    SELECT
        op.OperationId,
        op.ProvenanceStream.Scope,
        op.ProvenanceStream.Model,
        op.CreatedAt,
        op.ProvenanceStream.SegmentCount,
        pvr.OverallStatus,
        CASE pvr.OverallStatus
            WHEN 'PASS' THEN 1.0
            WHEN 'WARN' THEN 0.7
            WHEN 'FAIL' THEN 0.0
            ELSE 0.5
        END
    FROM dbo.OperationProvenance op
    LEFT JOIN dbo.ProvenanceValidationResults pvr ON op.OperationId = pvr.OperationId
    WHERE op.CreatedAt BETWEEN @StartDate AND @EndDate
    AND (@Scope IS NULL OR op.ProvenanceStream.Scope = @Scope);

    -- Calculate audit metrics
    DECLARE @TotalOperations INT = (SELECT COUNT(*) FROM @Operations);
    DECLARE @ValidOperations INT = (SELECT COUNT(*) FROM @Operations WHERE ValidationStatus = 'PASS');
    DECLARE @WarningOperations INT = (SELECT COUNT(*) FROM @Operations WHERE ValidationStatus = 'WARN');
    DECLARE @FailedOperations INT = (SELECT COUNT(*) FROM @Operations WHERE ValidationStatus = 'FAIL');
    DECLARE @AverageValidationScore FLOAT = (SELECT AVG(ValidationScore) FROM @Operations);
    DECLARE @AverageSegmentCount FLOAT = (SELECT AVG(SegmentCount) FROM @Operations);

    -- Check for anomalies
    DECLARE @Anomalies TABLE (AnomalyType NVARCHAR(100), Details NVARCHAR(MAX));

    -- Operations with no provenance
    IF EXISTS (SELECT 1 FROM dbo.OperationProvenance WHERE CreatedAt BETWEEN @StartDate AND @EndDate AND (ProvenanceStream IS NULL OR ProvenanceStream.IsNull = 1))
        INSERT INTO @Anomalies VALUES ('Missing Provenance', 'Operations found without provenance streams');

    -- Operations failing validation
    IF @FailedOperations > 0
        INSERT INTO @Anomalies VALUES ('Validation Failures', CAST(@FailedOperations AS NVARCHAR(10)) + ' operations failed validation');

    -- Operations with unusual segment counts
    DECLARE @AvgSegments FLOAT = (SELECT AVG(SegmentCount) FROM @Operations);
    DECLARE @StdDevSegments FLOAT = (SELECT STDEV(SegmentCount) FROM @Operations);

    IF @StdDevSegments IS NOT NULL AND EXISTS (SELECT 1 FROM @Operations WHERE ABS(SegmentCount - @AvgSegments) > 2 * @StdDevSegments)
        INSERT INTO @Anomalies VALUES ('Segment Count Anomalies', 'Operations with unusual number of provenance segments detected');

    -- Store audit result
    INSERT INTO dbo.ProvenanceAuditResults (
        AuditPeriodStart,
        AuditPeriodEnd,
        Scope,
        TotalOperations,
        ValidOperations,
        WarningOperations,
        FailedOperations,
        AverageValidationScore,
        AverageSegmentCount,
        Anomalies,
        AuditDurationMs,
        AuditedAt
    )
    VALUES (
        @StartDate,
        @EndDate,
        @Scope,
        @TotalOperations,
        @ValidOperations,
        @WarningOperations,
        @FailedOperations,
        @AverageValidationScore,
        @AverageSegmentCount,
        (SELECT * FROM @Anomalies FOR JSON PATH),
        DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()),
        SYSUTCDATETIME()
    );

    -- Return audit summary
    SELECT
        'Audit Summary' AS Metric,
        CAST(@StartDate AS NVARCHAR(30)) + ' - ' + CAST(@EndDate AS NVARCHAR(30)) AS Period,
        @TotalOperations AS TotalOperations,
        @ValidOperations AS ValidOperations,
        CAST(CAST(@ValidOperations AS FLOAT) / NULLIF(@TotalOperations, 0) * 100 AS DECIMAL(5,2)) AS ValidPercentage,
        @WarningOperations AS WarningOperations,
        @FailedOperations AS FailedOperations,
        CAST(@AverageValidationScore * 100 AS DECIMAL(5,2)) AS AvgValidationScore,
        CAST(@AverageSegmentCount AS DECIMAL(10,2)) AS AvgSegmentCount;

    -- Return anomalies
    SELECT
        AnomalyType,
        Details
    FROM @Anomalies;

    -- Return detailed operation results
    SELECT TOP 100
        OperationId,
        Scope,
        Model,
        CreatedAt,
        SegmentCount,
        ValidationStatus,
        CAST(ValidationScore * 100 AS DECIMAL(5,2)) AS ValidationScorePercent
    FROM @Operations
    ORDER BY CreatedAt DESC;

    IF @Debug = 1
        PRINT 'Provenance audit completed, analyzed ' + CAST(@TotalOperations AS NVARCHAR(10)) + ' operations';
END;
GO

-- sp_ReconstructOperationTimeline: Reconstruct operation timeline from provenance
CREATE OR ALTER PROCEDURE dbo.sp_ReconstructOperationTimeline
    @OperationId UNIQUEIDENTIFIER,
    @IncludePayloads BIT = 0,
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @Debug = 1
        PRINT 'Reconstructing operation timeline for ' + CAST(@OperationId AS NVARCHAR(36));

    -- Get operation provenance stream
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

    -- Return timeline
    IF @IncludePayloads = 1
    BEGIN
        -- Include full payload data
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
        PRINT 'Operation timeline reconstructed with ' + CAST(@SegmentCount AS NVARCHAR(10)) + ' segments';
END;
GO

PRINT 'Provenance tracking procedures created successfully';
PRINT 'sp_ValidateOperationProvenance: Validate individual operation provenance';
PRINT 'sp_AuditProvenanceChain: Comprehensive provenance audit';
PRINT 'sp_ReconstructOperationTimeline: Reconstruct operation timeline';
GO
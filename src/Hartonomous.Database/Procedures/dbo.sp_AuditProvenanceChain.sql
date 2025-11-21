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
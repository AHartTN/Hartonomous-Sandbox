-- Test: sp_GenerateText produces AtomicStream with proper segments and telemetry
-- Validates CLR TVF integration and provenance capture

-- Prerequisites: Models, atoms, and embeddings must exist
IF NOT EXISTS (SELECT 1 FROM dbo.Models WHERE ModelType LIKE '%language%' OR ModelType = 'text_generation')
BEGIN
    RAISERROR('No language models found; skipping sp_GenerateText test.', 16, 1);
    RETURN;
END;

-- Test basic text generation
DECLARE @prompt NVARCHAR(MAX) = N'The future of AI is';
DECLARE @maxTokens INT = 8;
DECLARE @temperature FLOAT = 0.7;
DECLARE @topK INT = 5;

BEGIN TRY
    EXEC dbo.sp_GenerateText
        @prompt = @prompt,
        @max_tokens = @maxTokens,
        @temperature = @temperature,
        @ModelIds = NULL,
        @top_k = @topK;

    -- Verify inference request was logged
    IF NOT EXISTS (
        SELECT 1
        FROM dbo.InferenceRequests
        WHERE TaskType = 'text_generation'
          AND JSON_VALUE(InputData, '$.prompt') = @prompt
          AND TotalDurationMs IS NOT NULL
          AND OutputData IS NOT NULL
    )
    BEGIN
        RAISERROR('InferenceRequests entry missing or incomplete for sp_GenerateText.', 16, 1);
    END;

    -- Verify generation stream was created
    IF NOT EXISTS (
        SELECT 1
        FROM provenance.GenerationStreams
        WHERE Scope = 'text_generation'
          AND Stream IS NOT NULL
    )
    BEGIN
        RAISERROR('GenerationStreams entry missing for sp_GenerateText.', 16, 1);
    END;

    -- Validate AtomicStream structure
    DECLARE @stream provenance.AtomicStream;
    SELECT TOP (1) @stream = Stream
    FROM provenance.GenerationStreams
    WHERE Scope = 'text_generation'
    ORDER BY CreatedAt DESC;

    IF @stream IS NULL
    BEGIN
        RAISERROR('AtomicStream is NULL in GenerationStreams.', 16, 1);
    END;

    -- Enumerate segments and validate types
    DECLARE @segments TABLE (
        SegmentOrdinal INT,
        SegmentKind NVARCHAR(32),
        TimestampUtc DATETIME2(3),
        ContentType NVARCHAR(128)
    );

    INSERT INTO @segments
    SELECT segment_ordinal, segment_kind, timestamp_utc, content_type
    FROM provenance.clr_AtomicStreamSegments(@stream);

    -- Expect: Input, Control, Embedding, Telemetry, Output
    DECLARE @segmentCount INT = (SELECT COUNT(*) FROM @segments);
    IF @segmentCount < 5
    BEGIN
        RAISERROR('AtomicStream does not contain expected segment count (expected >= 5, got %d).', 16, 1, @segmentCount);
    END;

    IF NOT EXISTS (SELECT 1 FROM @segments WHERE SegmentKind = 'Input')
    BEGIN
        RAISERROR('AtomicStream missing Input segment.', 16, 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM @segments WHERE SegmentKind = 'Control')
    BEGIN
        RAISERROR('AtomicStream missing Control segment.', 16, 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM @segments WHERE SegmentKind = 'Embedding')
    BEGIN
        RAISERROR('AtomicStream missing Embedding segment.', 16, 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM @segments WHERE SegmentKind = 'Telemetry')
    BEGIN
        RAISERROR('AtomicStream missing Telemetry segment.', 16, 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM @segments WHERE SegmentKind = 'Output')
    BEGIN
        RAISERROR('AtomicStream missing Output segment.', 16, 1);
    END;

    PRINT '✓ sp_GenerateText test passed: AtomicStream segments validated.';
END TRY
BEGIN CATCH
    DECLARE @errorMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR('sp_GenerateText test failed: %s', 16, 1, @errorMsg);
END CATCH;
GO

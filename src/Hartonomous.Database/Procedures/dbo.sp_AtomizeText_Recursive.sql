-- =============================================
-- sp_AtomizeText_Recursive: Fractal Text Atomization
-- GREENFIELD IMPLEMENTATION - No legacy migration logic
-- =============================================
-- Purpose: Decompose text into atomic units with N-Gram pattern recognition
-- Strategy: Check for multi-word patterns first, then fall back to single words
-- Indexing: Stores Hilbert M-value in AtomComposition.SpatialKey for cache locality
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[sp_AtomizeText_Recursive]
    @Text NVARCHAR(MAX),
    @ParentAtomId BIGINT = NULL,
    @TenantId INT = 0,
    @MaxNGramSize INT = 5,  -- Look for up to 5-word phrases
    @PromotionThreshold INT = 10, -- Promote pattern if seen ?10 times
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
    DECLARE @AtomsCreated INT = 0;
    DECLARE @CompositionsCreated INT = 0;
    DECLARE @PatternsPromoted INT = 0;

    -- Normalize text
    DECLARE @NormalizedText NVARCHAR(MAX) = LTRIM(RTRIM(@Text));

    -- Temp table for tokens
    CREATE TABLE #Tokens (
        SequenceIndex INT IDENTITY(0,1) PRIMARY KEY,
        TokenText NVARCHAR(256) NOT NULL,
        AtomId BIGINT NULL,
        ContentHash BINARY(32) NULL
    );

    -- STEP A: Tokenize (simple whitespace split - replace with proper tokenizer)
    INSERT INTO #Tokens (TokenText)
    SELECT value
    FROM STRING_SPLIT(@NormalizedText, ' ')
    WHERE LTRIM(RTRIM(value)) <> '';

    DECLARE @TotalTokens INT = @@ROWCOUNT;

    IF @Debug = 1
        PRINT '   Tokenized: ' + CAST(@TotalTokens AS VARCHAR(10)) + ' tokens';

    -- STEP B: Check for N-Grams in StructuralPatterns (descending from MaxNGramSize)
    DECLARE @NGramSize INT = @MaxNGramSize;
    
    WHILE @NGramSize >= 2
    BEGIN
        IF @Debug = 1
            PRINT '   Checking for ' + CAST(@NGramSize AS VARCHAR(2)) + '-grams...';

        -- Find sequences that match known patterns
        WITH NGrams AS (
            SELECT 
                t1.SequenceIndex AS StartIndex,
                STRING_AGG(t2.TokenText, ' ') WITHIN GROUP (ORDER BY t2.SequenceIndex) AS NGramText,
                HASHBYTES('SHA2_256', 
                    STRING_AGG(t2.TokenText, ' ') WITHIN GROUP (ORDER BY t2.SequenceIndex)
                ) AS NGramHash
            FROM #Tokens t1
            CROSS APPLY (
                SELECT TokenText, SequenceIndex
                FROM #Tokens t2
                WHERE t2.SequenceIndex BETWEEN t1.SequenceIndex AND t1.SequenceIndex + @NGramSize - 1
            ) t2
            GROUP BY t1.SequenceIndex
            HAVING COUNT(*) = @NGramSize
        )
        UPDATE t
        SET t.ContentHash = sp.PatternHash,
            t.AtomId = sp.PromotedAtomId
        FROM #Tokens t
        INNER JOIN NGrams ng ON t.SequenceIndex = ng.StartIndex
        INNER JOIN provenance.StructuralPatterns sp 
            ON sp.PatternHash = ng.NGramHash
            AND sp.TenantId = @TenantId
            AND sp.ShouldPromote = 1
            AND sp.PromotedAtomId IS NOT NULL;

        SET @NGramSize = @NGramSize - 1;
    END

    -- STEP C: Create Atoms for unknown tokens
    DECLARE @NewAtoms TABLE (
        ContentHash BINARY(32) PRIMARY KEY,
        AtomId BIGINT NOT NULL
    );

    MERGE INTO dbo.Atom AS Target
    USING (
        SELECT DISTINCT 
            TokenText,
            HASHBYTES('SHA2_256', TokenText) AS ContentHash,
            CAST(TokenText AS VARBINARY(64)) AS AtomicValue
        FROM #Tokens
        WHERE AtomId IS NULL  -- Only unknown tokens
    ) AS Source
    ON Target.ContentHash = Source.ContentHash 
        AND Target.TenantId = @TenantId
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (
            TenantId, 
            Modality, 
            Subtype, 
            ContentHash, 
            AtomicValue, 
            CanonicalText,
            ReferenceCount,
            SpatialKey  -- GREENFIELD: Set spatial key from day 1
        )
        VALUES (
            @TenantId,
            'text',
            'word',
            Source.ContentHash,
            Source.AtomicValue,
            Source.TokenText,
            0,
            NULL  -- Will be populated by composition spatial key
        )
    OUTPUT INSERTED.ContentHash, INSERTED.AtomId
    INTO @NewAtoms;

    SET @AtomsCreated = @@ROWCOUNT;

    -- Update tokens table with AtomIds
    UPDATE t
    SET t.AtomId = a.AtomId
    FROM #Tokens t
    INNER JOIN dbo.Atom a 
        ON a.ContentHash = HASHBYTES('SHA2_256', t.TokenText)
        AND a.TenantId = @TenantId
    WHERE t.AtomId IS NULL;

    -- STEP D: Create AtomComposition with Hilbert-indexed positions
    IF @ParentAtomId IS NOT NULL
    BEGIN
        INSERT INTO dbo.AtomComposition (
            ParentAtomId,
            ComponentAtomId,
            SequenceIndex,
            SpatialKey
        )
        SELECT 
            @ParentAtomId,
            t.AtomId,
            t.SequenceIndex,
            -- CRITICAL: Store Hilbert value in M dimension for cache locality
            geometry::STGeomFromText(
                'POINT (' +
                CAST(t.SequenceIndex AS VARCHAR(20)) + ' ' +  -- X = Position
                CAST(t.AtomId % 10000 AS VARCHAR(20)) + ' ' + -- Y = Value (scaled)
                '0 ' +  -- Z = Depth (0 for flat text)
                CAST(dbo.fn_ComputeHilbertValue(
                    geometry::Point(t.SequenceIndex, t.AtomId % 10000, 0, 0),
                    21  -- 21-bit precision
                ) AS VARCHAR(20)) +  -- M = Hilbert index
                ')',
                0
            )
        FROM #Tokens t
        WHERE t.AtomId IS NOT NULL
        ORDER BY dbo.fn_ComputeHilbertValue(
            geometry::Point(t.SequenceIndex, t.AtomId % 10000, 0, 0),
            21
        );  -- Pre-sort by Hilbert for RLE compression

        SET @CompositionsCreated = @@ROWCOUNT;
    END

    DROP TABLE #Tokens;

    DECLARE @DurationMs INT = DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME());

    IF @Debug = 1
    BEGIN
        SELECT 
            @TotalTokens AS TotalTokens,
            @AtomsCreated AS AtomsCreated,
            @CompositionsCreated AS CompositionsCreated,
            @DurationMs AS DurationMs;
    END

    RETURN 0;
END
GO

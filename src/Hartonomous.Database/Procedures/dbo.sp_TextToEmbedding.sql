-- Text to vector embedding pipeline - SELF-REFERENTIAL VERSION
-- Paradigm: The database queries itself for embeddings using its own registered models
-- Falls back to TF-IDF only if no embedding model is available
GO

CREATE PROCEDURE dbo.sp_TextToEmbedding
    @text NVARCHAR(MAX),
    @ModelName NVARCHAR(100) = NULL,
    @embedding VECTOR(1536) OUTPUT,
    @dimension INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @text IS NULL OR LTRIM(RTRIM(@text)) = ''
        THROW 50080, 'Input text is required for embedding generation.', 1;

    DECLARE @embeddingBaseDimension INT = 768;
    DECLARE @sqlVectorDimension INT = 1998;
    DECLARE @paddingApplied BIT = CASE WHEN @sqlVectorDimension > @embeddingBaseDimension THEN 1 ELSE 0 END;
    DECLARE @vocabularySize BIGINT = (SELECT COUNT(*) FROM dbo.TokenVocabulary);
    DECLARE @modelId INT = NULL;
    DECLARE @modelCapability NVARCHAR(50) = NULL;
    DECLARE @usedSelfReferentialModel BIT = 0;
    DECLARE @durationMs INT;

    DECLARE @startTime DATETIME2 = SYSUTCDATETIME();

    -- V3 REFACTOR: The self-referential embedding path has been removed as it was calling a non-existent stored procedure (sp_IngestAtom)
    -- and represented a broken, incomplete architectural pattern. This procedure now exclusively uses the TF-IDF fallback.
    -- A proper, V3-compliant self-referential embedding model will be implemented separately.

    -- FALLBACK: TF-IDF vocabulary projection (original implementation)
    SET @ModelName = COALESCE(@ModelName, 'tfidf_vocabulary_embedding_fallback');
    SET @modelId = NULL;

    DECLARE @normalized NVARCHAR(MAX) = LOWER(@text);
    DECLARE @punctuation NVARCHAR(50) = N'.,;:!?()[]{}"''`~|/\';
    SET @normalized = TRANSLATE(@normalized, @punctuation, REPLICATE(' ', LEN(@punctuation)));

    DECLARE @tokens TABLE (
        Token NVARCHAR(100) PRIMARY KEY,
        Frequency INT NOT NULL,
        TokenId INT NULL,
        CorpusFrequency BIGINT NULL,
        Dimension INT NULL,
        Weight FLOAT NULL
    );

    INSERT INTO @tokens (Token, Frequency)
    SELECT TokenValue, COUNT(*)
    FROM (
        SELECT LTRIM(RTRIM(value)) AS TokenValue
        FROM STRING_SPLIT(@normalized, ' ', 1)
    ) AS tokenized
    WHERE TokenValue IS NOT NULL AND TokenValue <> ''
    GROUP BY TokenValue;

    DECLARE @totalTokens INT = (SELECT SUM(Frequency) FROM @tokens);
    DECLARE @uniqueTokens INT = (SELECT COUNT(*) FROM @tokens);

    IF @totalTokens IS NULL OR @totalTokens = 0
        THROW 50081, 'Unable to derive tokens from input text.', 1;

    UPDATE t
    SET TokenId = tv.TokenId,
        CorpusFrequency = tv.Frequency
    FROM @tokens AS t
    CROSS APPLY (
        SELECT TOP (1)
            tv.TokenId,
            tv.Frequency
        FROM dbo.TokenVocabulary AS tv
        WHERE LOWER(tv.Token) = t.Token
        ORDER BY tv.Frequency DESC, tv.TokenId
    ) AS tv;

    UPDATE t
    SET Dimension = ((TokenId % @embeddingBaseDimension) + @embeddingBaseDimension) % @embeddingBaseDimension,
        Weight = CAST(Frequency AS FLOAT) * (
            CASE
                WHEN @vocabularySize IS NOT NULL AND @vocabularySize > 0 AND CorpusFrequency IS NOT NULL AND CorpusFrequency > 0
                    THEN LOG((@vocabularySize + 1.0) / (CorpusFrequency + 1.0)) + 1.0
                ELSE 1.0
            END
        )
    FROM @tokens AS t
    WHERE t.TokenId IS NOT NULL;

    UPDATE t
    SET Dimension = CAST(ABS(CONVERT(BIGINT, CHECKSUM(t.Token))) % @embeddingBaseDimension AS INT),
        Weight = CAST(Frequency AS FLOAT) * 0.25
    FROM @tokens AS t
    WHERE t.TokenId IS NULL;

    DECLARE @vector TABLE (
        Component INT PRIMARY KEY,
        Value FLOAT NOT NULL
    );

    INSERT INTO @vector (Component, Value)
    SELECT Dimension, SUM(Weight)
    FROM @tokens
    WHERE Dimension IS NOT NULL
    GROUP BY Dimension;

    DECLARE @norm FLOAT = (
        SELECT SQRT(SUM(Value * Value))
        FROM @vector
    );

    IF @norm IS NULL OR @norm = 0
        SET @norm = 1;

    UPDATE @vector
    SET Value = Value / @norm;

    -- Build embedding JSON array using chunked concatenation to avoid STRING_AGG 8000 byte limit
    DECLARE @embeddingJson NVARCHAR(MAX) = '[';
    DECLARE @chunkSize INT = 100;
    DECLARE @currentIdx INT = 0;
    DECLARE @chunk NVARCHAR(MAX);
    
    WHILE @currentIdx < @sqlVectorDimension
    BEGIN
        WITH NumberSeries AS (
            SELECT TOP (@chunkSize)
                @currentIdx + ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS idx
            FROM sys.all_objects
        )
        SELECT @chunk = STRING_AGG(
            CAST(
                CASE WHEN ns.idx < @embeddingBaseDimension THEN COALESCE(v.Value, 0.0) ELSE 0.0 END
                AS FLOAT
            ),
            ','
        ) WITHIN GROUP (ORDER BY ns.idx)
        FROM NumberSeries AS ns
        LEFT JOIN @vector AS v ON v.Component = ns.idx
        WHERE ns.idx < @sqlVectorDimension;
        
        IF @currentIdx > 0
            SET @embeddingJson = @embeddingJson + ',';
        
        SET @embeddingJson = @embeddingJson + @chunk;
        SET @currentIdx = @currentIdx + @chunkSize;
    END;
    
    SET @embeddingJson = @embeddingJson + ']';
    SET @embedding = TRY_CAST(@embeddingJson AS VECTOR(1536));

    IF @embedding IS NULL
        THROW 50082, 'Failed to construct embedding vector from vocabulary projection.', 1;

    SET @dimension = @embeddingBaseDimension;

    SET @durationMs = DATEDIFF(MILLISECOND, @startTime, SYSUTCDATETIME());
    DECLARE @knownTokens INT = (SELECT COUNT(*) FROM @tokens WHERE TokenId IS NOT NULL);
    DECLARE @unknownTokens INT = @uniqueTokens - @knownTokens;
    DECLARE @inputData NVARCHAR(MAX) = JSON_OBJECT(
        'text': @text,
        'token_count': @totalTokens,
        'unique_tokens': @uniqueTokens,
        'unknown_tokens': @unknownTokens
    );
    DECLARE @modelsUsed NVARCHAR(MAX) = JSON_ARRAY(JSON_OBJECT(
        'modelName': @ModelName,
        'modelId': @modelId,
        'dimension': @embeddingBaseDimension,
        'paddingApplied': @paddingApplied
    ));
    DECLARE @outputData NVARCHAR(MAX) = JSON_OBJECT('embedding': JSON_QUERY(@embeddingJson));
    DECLARE @outputMetadata NVARCHAR(MAX) = JSON_OBJECT(
        'embedding_dimensions': @embeddingBaseDimension,
        'token_count': @totalTokens,
        'unique_tokens': @uniqueTokens,
        'unknown_tokens': @unknownTokens,
        'normalization': 'l2',
        'strategy': 'tfidf_vocabulary_projection_fallback',
        'self_referential_attempted': @usedSelfReferentialModel
    );

    INSERT INTO dbo.InferenceRequest (
        TaskType,
        InputData,
        ModelsUsed,
        EnsembleStrategy,
        OutputData,
        OutputMetadata,
        TotalDurationMs
    )
    VALUES (
        'text_to_embedding',
        CONVERT(NVARCHAR(MAX), @inputData),
        CONVERT(NVARCHAR(MAX), @modelsUsed),
        'tfidf_vocabulary_projection',
        CONVERT(NVARCHAR(MAX), @outputData),
        CONVERT(NVARCHAR(MAX), @outputMetadata),
        @durationMs
    );
END;
GO

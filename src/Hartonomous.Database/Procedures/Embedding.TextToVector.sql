-- Text to vector embedding pipeline - SELF-REFERENTIAL VERSION
-- Paradigm: The database queries itself for embeddings using its own registered models
-- Falls back to TF-IDF only if no embedding model is available
GO

CREATE OR ALTER PROCEDURE dbo.sp_TextToEmbedding
    @text NVARCHAR(MAX),
    @ModelName NVARCHAR(100) = NULL,
    @embedding VECTOR(1998) OUTPUT,
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

    DECLARE @startTime DATETIME2 = SYSUTCDATETIME();

    -- PARADIGM-COMPLIANT: Query for best embedding model in the database
    -- This makes sp_TextToEmbedding self-referential - it uses the database's own models
    IF @ModelName IS NULL
    BEGIN
        -- Find the best embedding model registered in our system
        SELECT TOP(1)
            @modelId = m.ModelId,
            @ModelName = m.ModelName,
            @embeddingBaseDimension = COALESCE(
                TRY_CAST(JSON_VALUE(m.MetadataJson, '$.embeddingDimension') AS INT),
                768
            ),
            @modelCapability = JSON_VALUE(m.MetadataJson, '$.supportedTasks')
        FROM dbo.Models m
        WHERE m.IsActive = 1
            AND (
                JSON_VALUE(m.MetadataJson, '$.supportedTasks') LIKE '%embedding%'
                OR m.ModelName LIKE '%embed%'
                OR m.ModelName LIKE '%llama%'  -- LLaMA models support embeddings
                OR m.ModelName LIKE '%qwen%'   -- Qwen models support embeddings
            )
        ORDER BY
            -- Prefer models explicitly marked as embedding models
            CASE WHEN JSON_VALUE(m.MetadataJson, '$.supportedTasks') LIKE '%embedding%' THEN 1 ELSE 2 END,
            -- Prefer larger context windows (better quality)
            TRY_CAST(JSON_VALUE(m.MetadataJson, '$.maxInputLength') AS INT) DESC,
            -- Prefer newer models
            m.CreatedAt DESC;
    END
    ELSE
    BEGIN
        -- User specified a model - look it up
        SELECT
            @modelId = m.ModelId,
            @embeddingBaseDimension = COALESCE(
                TRY_CAST(JSON_VALUE(m.MetadataJson, '$.embeddingDimension') AS INT),
                768
            ),
            @modelCapability = JSON_VALUE(m.MetadataJson, '$.supportedTasks')
        FROM dbo.Models m
        WHERE m.ModelName = @ModelName
            AND m.IsActive = 1;
    END

    -- PARADIGM-COMPLIANT: If we found an embedding model, use the database's own inference engine
    IF @modelId IS NOT NULL
    BEGIN
        -- Self-referential embedding: The database generates its own embeddings using registered models
        -- This is the "AI queries itself" principle in action
        
        -- First, ingest the text as an atom so we can reference it
        DECLARE @textAtomId BIGINT;
        EXEC dbo.sp_IngestAtom
            @ContentType = 'text/plain',
            @Content = @text,
            @TenantId = 0,
            @AtomId = @textAtomId OUTPUT;

        -- Now use the database's own attention mechanism to generate embeddings
        -- This invokes sp_GenerateWithAttention which queries TensorAtoms.WeightsGeometry
        DECLARE @generationStreamId BIGINT;
        DECLARE @contextJson NVARCHAR(MAX) = JSON_OBJECT(
            'task': 'embedding',
            'pooling': 'mean',
            'normalize': 1
        );

        BEGIN TRY
            EXEC dbo.sp_GenerateWithAttention
                @ModelId = @modelId,
                @InputAtomIds = @textAtomId,
                @ContextJson = @contextJson,
                @MaxTokens = 1,  -- For embeddings, we just need the final hidden state
                @Temperature = 1.0,
                @AttentionHeads = 8,
                @TenantId = 0,
                @Debug = 0;

            -- Extract the embedding from the generation stream
            -- The attention mechanism stores embeddings in GenerationStreamSegments
            SELECT TOP(1)
                @embedding = seg.EmbeddingVector,
                @dimension = @embeddingBaseDimension,
                @usedSelfReferentialModel = 1
            FROM dbo.GenerationStreamSegments seg
            INNER JOIN dbo.GenerationStreams gs ON seg.GenerationStreamId = gs.GenerationStreamId
            WHERE gs.ModelId = @modelId
                AND seg.CreatedAt >= @startTime
                AND seg.EmbeddingVector IS NOT NULL
            ORDER BY seg.CreatedAt DESC;

            -- If we successfully got an embedding, we're done!
            IF @embedding IS NOT NULL
            BEGIN
                DECLARE @durationMs INT = DATEDIFF(MILLISECOND, @startTime, SYSUTCDATETIME());
                
                -- Log the self-referential embedding generation
                INSERT INTO dbo.InferenceRequests (
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
                    JSON_OBJECT('text': @text, 'text_atom_id': @textAtomId),
                    JSON_ARRAY(JSON_OBJECT('modelName': @ModelName, 'modelId': @modelId, 'dimension': @embeddingBaseDimension)),
                    'self_referential_attention',
                    JSON_OBJECT('embedding_dimension': @embeddingBaseDimension),
                    JSON_OBJECT(
                        'strategy': 'self_referential_database_inference',
                        'model_capability': @modelCapability,
                        'normalization': 'attention_pooling'
                    ),
                    @durationMs
                );

                RETURN; -- Success via self-referential model!
            END
        END TRY
        BEGIN CATCH
            -- If attention generation fails, fall through to TF-IDF backup
            PRINT 'Self-referential embedding failed: ' + ERROR_MESSAGE();
        END CATCH
    END

    -- FALLBACK: TF-IDF vocabulary projection (original implementation)
    -- Only used if no embedding model is available or self-referential generation failed
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

    DECLARE @embeddingJson NVARCHAR(MAX);

    WITH NumberSeries AS (
        SELECT TOP (@sqlVectorDimension)
            ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS idx
        FROM sys.all_objects
    )
    SELECT @embeddingJson = '[' +
        STRING_AGG(
            LTRIM(RTRIM(STR(
                CASE WHEN ns.idx < @embeddingBaseDimension THEN COALESCE(v.Value, 0.0) ELSE 0.0 END,
                38,
                12
            ))),
            ','
        ) WITHIN GROUP (ORDER BY ns.idx)
        + ']'
    FROM NumberSeries AS ns
    LEFT JOIN @vector AS v ON v.Component = ns.idx;

    SET @embedding = TRY_CAST(@embeddingJson AS VECTOR(1998));

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

    INSERT INTO dbo.InferenceRequests (
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
        TRY_CAST(@inputData AS JSON),
        TRY_CAST(@modelsUsed AS JSON),
        'tfidf_vocabulary_projection',
        TRY_CAST(@outputData AS JSON),
        TRY_CAST(@outputMetadata AS JSON),
        @durationMs
    );
END;
GO

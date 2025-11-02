-- =============================================
-- Advanced Text Generation with Multi-Model Ensemble
-- =============================================

USE Hartonomous;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GenerateText
    @prompt NVARCHAR(MAX),
    @max_tokens INT = 50,
    @temperature FLOAT = 1.0,
    @use_ensemble BIT = 1,
    @ModelIds NVARCHAR(MAX) = NULL  -- Comma-separated model IDs, NULL = all active models
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @start_time DATETIME2 = SYSUTCDATETIME();
    DECLARE @InferenceId BIGINT;
    DECLARE @generated_text NVARCHAR(MAX) = @prompt;

    -- Log the inference request
    INSERT INTO dbo.InferenceRequests (
        TaskType,
        InputData,
        ModelsUsed,
        EnsembleStrategy,
        InputMetadata
    )
    VALUES (
        'text_generation',
        @prompt,
        CASE WHEN @use_ensemble = 1 THEN 'ensemble' ELSE 'single' END,
        CASE WHEN @use_ensemble = 1 THEN 'weighted_average' ELSE 'direct' END,
        JSON_OBJECT('max_tokens': @max_tokens, 'temperature': @temperature)
    );
    SET @InferenceId = SCOPE_IDENTITY();

    -- Get active models for ensemble
    DECLARE @models TABLE (ModelId INT, ModelName NVARCHAR(200), Weight FLOAT);
    IF @use_ensemble = 1
    BEGIN
        IF @ModelIds IS NULL
        BEGIN
            -- Use all active models with equal weights
            INSERT INTO @models
            SELECT ModelId, ModelName, 1.0
            FROM dbo.Models_Production
            WHERE IsActive = 1;
        END
        ELSE
        BEGIN
            -- Use specified models
            INSERT INTO @models
            SELECT mp.ModelId, mp.ModelName, 1.0
            FROM dbo.Models_Production mp
            WHERE mp.IsActive = 1
              AND mp.ModelId IN (SELECT value FROM STRING_SPLIT(@ModelIds, ','));
        END
    END

    -- Tokenize prompt (simplified tokenization)
    DECLARE @tokens TABLE (Token NVARCHAR(100), Position INT IDENTITY(1,1));
    INSERT INTO @tokens (Token)
    SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@prompt, ' ');

    DECLARE @current_position INT = (SELECT COUNT(*) FROM @tokens);
    DECLARE @i INT = 0;

    WHILE @i < @max_tokens
    BEGIN
        DECLARE @next_token NVARCHAR(100);

        IF @use_ensemble = 1 AND (SELECT COUNT(*) FROM @models) > 1
        BEGIN
            -- Multi-model ensemble prediction
            SELECT TOP 1 @next_token = Token
            FROM (
                SELECT
                    tv.Token,
                    SUM(EnsembleScore) as TotalScore
                FROM (
                    -- Get predictions from each model
                    SELECT
                        tv.Token,
                        m.Weight * (1.0 - VECTOR_DISTANCE('cosine', tv.Embedding, ContextEmbedding)) as EnsembleScore,
                        ROW_NUMBER() OVER (PARTITION BY m.ModelId ORDER BY VECTOR_DISTANCE('cosine', tv.Embedding, ContextEmbedding)) as RankInModel
                    FROM @models m
                    CROSS APPLY (
                        -- Get context embedding (simplified: average of recent tokens)
                        SELECT TOP 5 Embedding
                        FROM dbo.TokenVocabulary tv_context
                        WHERE tv_context.Token IN (
                            SELECT TOP 5 Token FROM @tokens ORDER BY Position DESC
                        )
                    ) context(ContextEmbedding)
                    CROSS JOIN dbo.TokenVocabulary tv
                    WHERE tv.Token NOT IN (SELECT Token FROM @tokens) -- Avoid repetition
                ) model_predictions
                JOIN dbo.TokenVocabulary tv ON tv.Token = model_predictions.Token
                WHERE RankInModel <= 3  -- Top 3 from each model
                GROUP BY tv.Token
            ) ensemble_results
            ORDER BY TotalScore DESC;
        END
        ELSE
        BEGIN
            -- Single model prediction (fallback)
            SELECT TOP 1 @next_token = tv.Token
            FROM dbo.TokenVocabulary tv
            CROSS APPLY (
                SELECT TOP 1 Embedding as ContextEmbedding
                FROM dbo.TokenVocabulary tv_context
                WHERE tv_context.Token IN (
                    SELECT TOP 3 Token FROM @tokens ORDER BY Position DESC
                )
            ) context
            WHERE tv.Token NOT IN (SELECT Token FROM @tokens)
            ORDER BY VECTOR_DISTANCE('cosine', tv.Embedding, ContextEmbedding);
        END

        -- Apply temperature (add randomness)
        IF @temperature < 1.0
        BEGIN
            -- Deterministic: take the best token
            SET @next_token = @next_token;
        END
        ELSE IF @temperature > 1.0
        BEGIN
            -- Random sampling from top candidates
            DECLARE @candidates TABLE (Token NVARCHAR(100), Score FLOAT);
            INSERT INTO @candidates
            SELECT TOP 5 tv.Token, (1.0 - VECTOR_DISTANCE('cosine', tv.Embedding, ContextEmbedding)) as Score
            FROM dbo.TokenVocabulary tv
            CROSS APPLY (
                SELECT TOP 1 Embedding as ContextEmbedding
                FROM dbo.TokenVocabulary tv_context
                WHERE tv_context.Token IN (
                    SELECT TOP 3 Token FROM @tokens ORDER BY Position DESC
                )
            ) context
            ORDER BY Score DESC;

            -- Weighted random selection
            DECLARE @rand FLOAT = RAND();
            DECLARE @cumulative FLOAT = 0;

            SELECT @next_token = Token
            FROM (
                SELECT Token, Score / (SELECT SUM(Score) FROM @candidates) as NormalizedScore
                FROM @candidates
            ) normalized
            WHERE @rand BETWEEN @cumulative AND @cumulative + NormalizedScore
            ORDER BY NormalizedScore DESC;
        END

        -- Stop if no valid next token
        IF @next_token IS NULL OR @next_token = ''
            BREAK;

        -- Add token to sequence
        SET @generated_text = @generated_text + ' ' + @next_token;
        INSERT INTO @tokens (Token) VALUES (@next_token);
        SET @current_position = @current_position + 1;

        -- Check for end-of-sequence
        IF @next_token IN ('[EOS]', '</s>', '<|endoftext|>')
            BREAK;

        SET @i = @i + 1;
    END

    -- Calculate duration and update inference request
    DECLARE @DurationMs INT = DATEDIFF(MILLISECOND, @start_time, SYSUTCDATETIME());
    DECLARE @tokens_generated INT = @i;

    UPDATE dbo.InferenceRequests
    SET TotalDurationMs = @DurationMs,
        OutputMetadata = JSON_OBJECT(
            'status': 'completed',
            'tokens_generated': @tokens_generated,
            'total_length': LEN(@generated_text),
            'temperature': @temperature,
            'ensemble_used': @use_ensemble
        )
    WHERE InferenceId = @InferenceId;

    -- Log inference steps
    INSERT INTO dbo.InferenceSteps (InferenceId, StepNumber, ModelId, OperationType, DurationMs, Metadata)
    SELECT
        @InferenceId,
        ROW_NUMBER() OVER (ORDER BY m.ModelId),
        m.ModelId,
        'text_generation',
        @DurationMs / (SELECT COUNT(*) FROM @models),
        JSON_OBJECT('tokens_generated': @tokens_generated)
    FROM @models m;

    -- Return results
    SELECT
        @InferenceId as InferenceId,
        @prompt as OriginalPrompt,
        @generated_text as GeneratedText,
        @tokens_generated as TokensGenerated,
        @DurationMs as DurationMs,
        CASE WHEN @use_ensemble = 1 THEN 'MULTI_MODEL_ENSEMBLE' ELSE 'SINGLE_MODEL' END as Method;
END
GO
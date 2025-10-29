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
    @model_ids NVARCHAR(MAX) = NULL  -- Comma-separated model IDs, NULL = all active models
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @start_time DATETIME2 = SYSUTCDATETIME();
    DECLARE @inference_id BIGINT;
    DECLARE @generated_text NVARCHAR(MAX) = @prompt;

    -- Log the inference request
    INSERT INTO dbo.InferenceRequests (
        task_type,
        input_data,
        models_used,
        ensemble_strategy,
        input_metadata
    )
    VALUES (
        'text_generation',
        @prompt,
        CASE WHEN @use_ensemble = 1 THEN 'ensemble' ELSE 'single' END,
        CASE WHEN @use_ensemble = 1 THEN 'weighted_average' ELSE 'direct' END,
        JSON_OBJECT('max_tokens': @max_tokens, 'temperature': @temperature)
    );
    SET @inference_id = SCOPE_IDENTITY();

    -- Get active models for ensemble
    DECLARE @models TABLE (model_id INT, model_name NVARCHAR(200), weight FLOAT);
    IF @use_ensemble = 1
    BEGIN
        IF @model_ids IS NULL
        BEGIN
            -- Use all active models with equal weights
            INSERT INTO @models
            SELECT model_id, model_name, 1.0
            FROM dbo.Models_Production
            WHERE is_active = 1;
        END
        ELSE
        BEGIN
            -- Use specified models
            INSERT INTO @models
            SELECT mp.model_id, mp.model_name, 1.0
            FROM dbo.Models_Production mp
            WHERE mp.is_active = 1
              AND mp.model_id IN (SELECT value FROM STRING_SPLIT(@model_ids, ','));
        END
    END

    -- Tokenize prompt (simplified tokenization)
    DECLARE @tokens TABLE (token NVARCHAR(100), position INT IDENTITY(1,1));
    INSERT INTO @tokens (token)
    SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@prompt, ' ');

    DECLARE @current_position INT = (SELECT COUNT(*) FROM @tokens);
    DECLARE @i INT = 0;

    WHILE @i < @max_tokens
    BEGIN
        DECLARE @next_token NVARCHAR(100);

        IF @use_ensemble = 1 AND (SELECT COUNT(*) FROM @models) > 1
        BEGIN
            -- Multi-model ensemble prediction
            SELECT TOP 1 @next_token = token
            FROM (
                SELECT
                    tv.token,
                    SUM(ensemble_score) as total_score
                FROM (
                    -- Get predictions from each model
                    SELECT
                        tv.token,
                        m.weight * (1.0 - VECTOR_DISTANCE('cosine', tv.embedding, context_embedding)) as ensemble_score,
                        ROW_NUMBER() OVER (PARTITION BY m.model_id ORDER BY VECTOR_DISTANCE('cosine', tv.embedding, context_embedding)) as rank_in_model
                    FROM @models m
                    CROSS APPLY (
                        -- Get context embedding (simplified: average of recent tokens)
                        SELECT TOP 5 embedding
                        FROM dbo.TokenVocabulary tv_context
                        WHERE tv_context.token IN (
                            SELECT TOP 5 token FROM @tokens ORDER BY position DESC
                        )
                    ) context(context_embedding)
                    CROSS JOIN dbo.TokenVocabulary tv
                    WHERE tv.token NOT IN (SELECT token FROM @tokens) -- Avoid repetition
                ) model_predictions
                JOIN dbo.TokenVocabulary tv ON tv.token = model_predictions.token
                WHERE rank_in_model <= 3  -- Top 3 from each model
                GROUP BY tv.token
            ) ensemble_results
            ORDER BY total_score DESC;
        END
        ELSE
        BEGIN
            -- Single model prediction (fallback)
            SELECT TOP 1 @next_token = tv.token
            FROM dbo.TokenVocabulary tv
            CROSS APPLY (
                SELECT TOP 1 embedding as context_embedding
                FROM dbo.TokenVocabulary tv_context
                WHERE tv_context.token IN (
                    SELECT TOP 3 token FROM @tokens ORDER BY position DESC
                )
            ) context
            WHERE tv.token NOT IN (SELECT token FROM @tokens)
            ORDER BY VECTOR_DISTANCE('cosine', tv.embedding, context_embedding);
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
            DECLARE @candidates TABLE (token NVARCHAR(100), score FLOAT);
            INSERT INTO @candidates
            SELECT TOP 5 tv.token, (1.0 - VECTOR_DISTANCE('cosine', tv.embedding, context_embedding)) as score
            FROM dbo.TokenVocabulary tv
            CROSS APPLY (
                SELECT TOP 1 embedding as context_embedding
                FROM dbo.TokenVocabulary tv_context
                WHERE tv_context.token IN (
                    SELECT TOP 3 token FROM @tokens ORDER BY position DESC
                )
            ) context
            ORDER BY score DESC;

            -- Weighted random selection
            DECLARE @rand FLOAT = RAND();
            DECLARE @cumulative FLOAT = 0;

            SELECT @next_token = token
            FROM (
                SELECT token, score / (SELECT SUM(score) FROM @candidates) as normalized_score
                FROM @candidates
            ) normalized
            WHERE @rand BETWEEN @cumulative AND @cumulative + normalized_score
            ORDER BY normalized_score DESC;
        END

        -- Stop if no valid next token
        IF @next_token IS NULL OR @next_token = ''
            BREAK;

        -- Add token to sequence
        SET @generated_text = @generated_text + ' ' + @next_token;
        INSERT INTO @tokens (token) VALUES (@next_token);
        SET @current_position = @current_position + 1;

        -- Check for end-of-sequence
        IF @next_token IN ('[EOS]', '</s>', '<|endoftext|>')
            BREAK;

        SET @i = @i + 1;
    END

    -- Calculate duration and update inference request
    DECLARE @duration_ms INT = DATEDIFF(MILLISECOND, @start_time, SYSUTCDATETIME());
    DECLARE @tokens_generated INT = @i;

    UPDATE dbo.InferenceRequests
    SET total_duration_ms = @duration_ms,
        output_metadata = JSON_OBJECT(
            'status': 'completed',
            'tokens_generated': @tokens_generated,
            'total_length': LEN(@generated_text),
            'temperature': @temperature,
            'ensemble_used': @use_ensemble
        )
    WHERE inference_id = @inference_id;

    -- Log inference steps
    INSERT INTO dbo.InferenceSteps (inference_id, step_number, model_id, operation_type, duration_ms, metadata)
    SELECT
        @inference_id,
        ROW_NUMBER() OVER (ORDER BY m.model_id),
        m.model_id,
        'text_generation',
        @duration_ms / (SELECT COUNT(*) FROM @models),
        JSON_OBJECT('tokens_generated': @tokens_generated)
    FROM @models m;

    -- Return results
    SELECT
        @inference_id as inference_id,
        @prompt as original_prompt,
        @generated_text as generated_text,
        @tokens_generated as tokens_generated,
        @duration_ms as duration_ms,
        CASE WHEN @use_ensemble = 1 THEN 'MULTI_MODEL_ENSEMBLE' ELSE 'SINGLE_MODEL' END as method;
END
GO
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

    -- Log the inference request (using correct schema)
    INSERT INTO dbo.InferenceRequests (
        TaskType,
        InputData,
        ModelsUsed,
        OutputMetadata
    )
    VALUES (
        'text_generation',
        JSON_OBJECT(
            'prompt': @prompt,
            'max_tokens': @max_tokens,
            'temperature': @temperature
        ),
        CASE WHEN @use_ensemble = 1 THEN JSON_OBJECT('strategy': 'ensemble') ELSE JSON_OBJECT('strategy': 'single') END,
        JSON_OBJECT('status': 'started')
    );
    SET @InferenceId = SCOPE_IDENTITY();

    -- Get active models for ensemble (use Models table, not Models_Production)
    DECLARE @models TABLE (ModelId INT, ModelName NVARCHAR(200), Weight FLOAT);
    IF @use_ensemble = 1
    BEGIN
        IF @ModelIds IS NULL
        BEGIN
            -- Use all models with equal weights
            INSERT INTO @models
            SELECT ModelId, ModelName, 1.0
            FROM dbo.Models
            WHERE ModelType IN ('neural_network', 'vocabulary');
        END
        ELSE
        BEGIN
            -- Use specified models
            INSERT INTO @models
            SELECT m.ModelId, m.ModelName, 1.0
            FROM dbo.Models m
            WHERE m.ModelType IN ('neural_network', 'vocabulary')
              AND m.ModelId IN (SELECT CAST(value AS INT) FROM STRING_SPLIT(@ModelIds, ','));
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
            -- Multi-model ensemble prediction using AtomEmbeddings
            SELECT TOP 1 @next_token = Token
            FROM (
                SELECT
                    CAST(a.AtomData AS NVARCHAR(100)) AS Token,
                    SUM(EnsembleScore) as TotalScore
                FROM (
                    -- Get predictions from each model via spatial similarity
                    SELECT
                        ae.AtomId,
                        m.Weight * (1.0 - ae.SpatialGeometry.STDistance(context.ContextSpatial)) as EnsembleScore,
                        ROW_NUMBER() OVER (PARTITION BY m.ModelId ORDER BY ae.SpatialGeometry.STDistance(context.ContextSpatial)) as RankInModel
                    FROM @models m
                    CROSS APPLY (
                        -- Get context spatial centroid from recent tokens
                        SELECT TOP 1 geometry::STGeomFromText(
                            'POINT(' +
                            CAST(AVG(ae_ctx.SpatialGeometry.STX) AS NVARCHAR(50)) + ' ' +
                            CAST(AVG(ae_ctx.SpatialGeometry.STY) AS NVARCHAR(50)) + ' ' +
                            CAST(AVG(CAST(COALESCE(ae_ctx.SpatialGeometry.Z, 0) AS FLOAT)) AS NVARCHAR(50)) + ')',
                            0
                        ) AS ContextSpatial
                        FROM @tokens t_ctx
                        INNER JOIN dbo.Atoms a_ctx ON CAST(a_ctx.AtomData AS NVARCHAR(100)) = t_ctx.Token
                        INNER JOIN dbo.AtomEmbeddings ae_ctx ON ae_ctx.AtomId = a_ctx.AtomId
                        ORDER BY t_ctx.Position DESC
                    ) context
                    INNER JOIN dbo.AtomEmbeddings ae ON ae.SpatialGeometry IS NOT NULL
                    WHERE ae.AtomId NOT IN (
                        SELECT a_existing.AtomId FROM @tokens t_existing
                        INNER JOIN dbo.Atoms a_existing ON CAST(a_existing.AtomData AS NVARCHAR(100)) = t_existing.Token
                    )
                ) model_predictions
                INNER JOIN dbo.Atoms a ON a.AtomId = model_predictions.AtomId
                WHERE RankInModel <= 3  -- Top 3 from each model
                GROUP BY a.AtomId, a.AtomData
            ) ensemble_results
            ORDER BY TotalScore DESC;
        END
        ELSE
        BEGIN
            -- Single model prediction (fallback) using AtomEmbeddings
            SELECT TOP 1 @next_token = CAST(a.AtomData AS NVARCHAR(100))
            FROM dbo.AtomEmbeddings ae
            INNER JOIN dbo.Atoms a ON ae.AtomId = a.AtomId
            CROSS APPLY (
                SELECT TOP 1 geometry::STGeomFromText(
                    'POINT(' +
                    CAST(AVG(ae_ctx.SpatialGeometry.STX) AS NVARCHAR(50)) + ' ' +
                    CAST(AVG(ae_ctx.SpatialGeometry.STY) AS NVARCHAR(50)) + ' ' +
                    CAST(AVG(CAST(COALESCE(ae_ctx.SpatialGeometry.Z, 0) AS FLOAT)) AS NVARCHAR(50)) + ')',
                    0
                ) AS ContextSpatial
                FROM @tokens t_ctx
                INNER JOIN dbo.Atoms a_ctx ON CAST(a_ctx.AtomData AS NVARCHAR(100)) = t_ctx.Token
                INNER JOIN dbo.AtomEmbeddings ae_ctx ON ae_ctx.AtomId = a_ctx.AtomId
                ORDER BY t_ctx.Position DESC
            ) context
            WHERE ae.AtomId NOT IN (
                SELECT a_existing.AtomId FROM @tokens t_existing
                INNER JOIN dbo.Atoms a_existing ON CAST(a_existing.AtomData AS NVARCHAR(100)) = t_existing.Token
            )
            AND ae.SpatialGeometry IS NOT NULL
            ORDER BY ae.SpatialGeometry.STDistance(context.ContextSpatial);
        END

        -- Apply temperature (add randomness)
        IF @temperature < 1.0
        BEGIN
            -- Deterministic: take the best token
            SET @next_token = @next_token;
        END
        ELSE IF @temperature > 1.0
        BEGIN
            -- Random sampling from top candidates using AtomEmbeddings
            DECLARE @candidates TABLE (Token NVARCHAR(100), Score FLOAT);
            
            INSERT INTO @candidates
            SELECT TOP 5 
                CAST(a.AtomData AS NVARCHAR(100)) AS Token,
                (1.0 - ae.SpatialGeometry.STDistance(context.ContextSpatial)) as Score
            FROM dbo.AtomEmbeddings ae
            INNER JOIN dbo.Atoms a ON ae.AtomId = a.AtomId
            CROSS APPLY (
                SELECT TOP 1 geometry::STGeomFromText(
                    'POINT(' +
                    CAST(AVG(ae_ctx.SpatialGeometry.STX) AS NVARCHAR(50)) + ' ' +
                    CAST(AVG(ae_ctx.SpatialGeometry.STY) AS NVARCHAR(50)) + ' ' +
                    CAST(AVG(CAST(COALESCE(ae_ctx.SpatialGeometry.Z, 0) AS FLOAT)) AS NVARCHAR(50)) + ')',
                    0
                ) AS ContextSpatial
                FROM @tokens t_ctx
                INNER JOIN dbo.Atoms a_ctx ON CAST(a_ctx.AtomData AS NVARCHAR(100)) = t_ctx.Token
                INNER JOIN dbo.AtomEmbeddings ae_ctx ON ae_ctx.AtomId = a_ctx.AtomId
                ORDER BY t_ctx.Position DESC
            ) context
            WHERE ae.SpatialGeometry IS NOT NULL
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
    INSERT INTO dbo.InferenceSteps (InferenceId, StepNumber, ModelId, OperationType, DurationMs)
    SELECT
        @InferenceId,
        ROW_NUMBER() OVER (ORDER BY m.ModelId),
        m.ModelId,
        'text_generation',
        @DurationMs / (SELECT COUNT(*) FROM @models)
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

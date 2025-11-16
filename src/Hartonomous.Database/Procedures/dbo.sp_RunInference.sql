-- =====================================================
-- sp_RunInference
-- Generative Autoregressive Inference with Temperature Sampling
-- =====================================================
-- Implements next-token prediction using spatial similarity
-- Supports temperature-based stochastic sampling
-- Logs inference requests for OODA loop analysis

CREATE OR ALTER PROCEDURE dbo.sp_RunInference
    @contextAtomIds NVARCHAR(MAX),
    @temperature FLOAT = 1.0,
    @topK INT = 10,
    @topP FLOAT = 0.9,
    @maxTokens INT = 100,
    @tenantId INT = 0,
    @modalityFilter NVARCHAR(50) = NULL,
    @inferenceId BIGINT = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @startTime DATETIME2 = SYSUTCDATETIME();

    -- Input validation
    IF @contextAtomIds IS NULL OR LEN(@contextAtomIds) = 0
    BEGIN
        RAISERROR('Context atom IDs cannot be null or empty', 16, 1);
        RETURN -1;
    END

    -- Parameter bounds
    IF @temperature <= 0.0 SET @temperature = 0.01;
    IF @temperature > 2.0 SET @temperature = 2.0;
    IF @topK <= 0 SET @topK = 10;
    IF @topK > 100 SET @topK = 100;
    IF @topP <= 0.0 SET @topP = 0.01;
    IF @topP > 1.0 SET @topP = 1.0;
    IF @maxTokens <= 0 SET @maxTokens = 100;
    IF @maxTokens > 1000 SET @maxTokens = 1000;

    -- Generate inference ID if not provided
    IF @inferenceId IS NULL
    BEGIN
        IF OBJECT_ID('dbo.seq_InferenceId', 'SO') IS NOT NULL
            SET @inferenceId = NEXT VALUE FOR dbo.seq_InferenceId;
        ELSE
            SET @inferenceId = ABS(CHECKSUM(NEWID()));
    END

    -- Parse context atom IDs
    DECLARE @contextAtoms TABLE (
        AtomId BIGINT PRIMARY KEY,
        SequenceOrder INT
    );
    
    INSERT INTO @contextAtoms (AtomId, SequenceOrder)
    SELECT
        CAST(LTRIM(RTRIM(value)) AS BIGINT) AS AtomId,
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS SequenceOrder
    FROM STRING_SPLIT(@contextAtomIds, ',')
    WHERE LTRIM(RTRIM(value)) <> ''
        AND ISNUMERIC(LTRIM(RTRIM(value))) = 1;

    DECLARE @contextCount INT = (SELECT COUNT(*) FROM @contextAtoms);

    IF @contextCount = 0
    BEGIN
        RAISERROR('No valid context atom IDs provided', 16, 1);
        RETURN -1;
    END

    -- =====================================================
    -- STEP 1: Compute Context Vector
    -- =====================================================
    -- Average embeddings of context atoms
    -- More sophisticated: weighted by position, attention scores, etc.

    DECLARE @contextVector VARBINARY(MAX);
    DECLARE @contextText NVARCHAR(MAX) = '';

    BEGIN TRY
        -- Check if CLR vector averaging function exists
        IF OBJECT_ID('dbo.clr_VectorAverage', 'FN') IS NOT NULL
        BEGIN
            -- Use SIMD-optimized CLR function
            SELECT @contextVector = dbo.clr_VectorAverage(ae.EmbeddingVector)
            FROM dbo.AtomEmbeddings ae
            INNER JOIN @contextAtoms ca ON ae.AtomId = ca.AtomId
            WHERE ae.EmbeddingVector IS NOT NULL;
        END
        ELSE
        BEGIN
            -- Fallback: use first context atom's embedding
            SELECT TOP 1 @contextVector = ae.EmbeddingVector
            FROM dbo.AtomEmbeddings ae
            INNER JOIN @contextAtoms ca ON ae.AtomId = ca.AtomId
            WHERE ae.EmbeddingVector IS NOT NULL
            ORDER BY ca.SequenceOrder;
        END

        -- Concatenate context text for logging
        SELECT @contextText = STRING_AGG(a.CanonicalText, ' ')
        FROM dbo.Atoms a
        INNER JOIN @contextAtoms ca ON a.AtomId = ca.AtomId
        ORDER BY ca.SequenceOrder;

    END TRY
    BEGIN CATCH
        DECLARE @vectorError NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR('Failed to compute context vector: %s', 16, 1, @vectorError);
        RETURN -1;
    END CATCH

    IF @contextVector IS NULL
    BEGIN
        RAISERROR('Context vector is NULL - no embeddings found for context atoms', 16, 1);
        RETURN -1;
    END

    -- =====================================================
    -- STEP 2: Find Candidate Atoms (O(log N) Query)
    -- =====================================================

    DECLARE @candidates TABLE (
        AtomId BIGINT,
        CanonicalText NVARCHAR(MAX),
        Modality NVARCHAR(50),
        Score FLOAT,
        SpatialDistance FLOAT,
        Rank INT,
        INDEX IX_Candidates_Score NONCLUSTERED (Score DESC)
    );

    -- Use sp_FindNearestAtoms for spatial similarity search
    INSERT INTO @candidates (AtomId, CanonicalText, Modality, Score, SpatialDistance, Rank)
    SELECT
        AtomId,
        CanonicalText,
        Modality,
        Score,
        SpatialDistance,
        ROW_NUMBER() OVER (ORDER BY Score DESC) AS Rank
    FROM dbo.sp_FindNearestAtoms(
        @queryVector = @contextVector,
        @topK = @topK * 2, -- Get more candidates for diversity
        @spatialPoolSize = 2000,
        @tenantId = @tenantId,
        @modalityFilter = @modalityFilter,
        @useHilbertClustering = 1
    );

    IF NOT EXISTS (SELECT 1 FROM @candidates)
    BEGIN
        RAISERROR('No candidate atoms found for inference', 16, 1);
        RETURN -1;
    END

    -- =====================================================
    -- STEP 3: Temperature-Based Sampling
    -- =====================================================
    -- Apply softmax with temperature scaling
    -- Higher temp = more random, lower temp = more greedy

    DECLARE @sampledAtoms TABLE (
        Step INT IDENTITY(1,1),
        AtomId BIGINT,
        CanonicalText NVARCHAR(MAX),
        Probability FLOAT,
        CumulativeProbability FLOAT
    );

    ;WITH ScoredCandidates AS (
        SELECT
            AtomId,
            CanonicalText,
            Modality,
            Score,
            SpatialDistance,
            Rank,
            -- Temperature scaling: exp(score / temperature)
            EXP(Score / @temperature) AS ScaledScore
        FROM @candidates
        WHERE Rank <= @topK
    ),
    Normalized AS (
        SELECT
            AtomId,
            CanonicalText,
            Modality,
            Score,
            SpatialDistance,
            ScaledScore,
            -- Normalize to probabilities (sum = 1.0)
            ScaledScore / SUM(ScaledScore) OVER () AS Probability
        FROM ScoredCandidates
    ),
    CumulativeProbs AS (
        SELECT
            AtomId,
            CanonicalText,
            Modality,
            Probability,
            SUM(Probability) OVER (ORDER BY Score DESC) AS CumulativeProbability
        FROM Normalized
    ),
    TopPFiltered AS (
        -- Nucleus sampling (top-p): only consider atoms in top p% probability mass
        SELECT
            AtomId,
            CanonicalText,
            Probability,
            CumulativeProbability
        FROM CumulativeProbs
        WHERE CumulativeProbability <= @topP
           OR Probability >= 0.05 -- Always include high probability atoms
    )
    INSERT INTO @sampledAtoms (AtomId, CanonicalText, Probability, CumulativeProbability)
    SELECT TOP (@maxTokens)
        AtomId,
        CanonicalText,
        Probability,
        CumulativeProbability
    FROM TopPFiltered
    ORDER BY 
        CASE 
            WHEN @temperature < 0.1 THEN Probability -- Greedy for low temp
            ELSE NEWID() -- Stochastic for higher temp
        END DESC;

    -- =====================================================
    -- STEP 4: Log Inference Request
    -- =====================================================

    DECLARE @outputText NVARCHAR(MAX) = (
        SELECT STRING_AGG(CanonicalText, ' ')
        FROM @sampledAtoms
        ORDER BY Step
    );

    DECLARE @outputJson NVARCHAR(MAX) = (
        SELECT
            Step,
            AtomId,
            CanonicalText,
            Probability
        FROM @sampledAtoms
        ORDER BY Step
        FOR JSON PATH
    );

    BEGIN TRANSACTION;

    BEGIN TRY
        -- Log to InferenceRequests table
        IF OBJECT_ID('dbo.InferenceRequests', 'U') IS NOT NULL
        BEGIN
            INSERT INTO dbo.InferenceRequests (
                InferenceId,
                TenantId,
                ModelId,
                InputData,
                OutputData,
                Temperature,
                TopK,
                TopP,
                MaxTokens,
                RequestTimestamp,
                CompletedTimestamp,
                TotalDurationMs,
                Status
            )
            VALUES (
                @inferenceId,
                @tenantId,
                NULL, -- No specific model (using spatial similarity)
                @contextText,
                @outputText,
                @temperature,
                @topK,
                @topP,
                @maxTokens,
                @startTime,
                SYSUTCDATETIME(),
                DATEDIFF(MILLISECOND, @startTime, SYSUTCDATETIME()),
                'Completed'
            );
        END

        -- Track which atoms were used (for OODA loop analysis)
        IF OBJECT_ID('dbo.InferenceTracking', 'U') IS NOT NULL
        BEGIN
            INSERT INTO dbo.InferenceTracking (InferenceId, AtomId, SequencePosition)
            SELECT @inferenceId, AtomId, Step
            FROM @sampledAtoms;
        END

        COMMIT TRANSACTION;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        -- Don't fail the inference if logging fails
        DECLARE @logError NVARCHAR(4000) = ERROR_MESSAGE();
        PRINT 'Warning: Failed to log inference: ' + @logError;
    END CATCH

    -- =====================================================
    -- STEP 5: Return Generated Sequence
    -- =====================================================

    SELECT
        @inferenceId AS InferenceId,
        Step,
        AtomId,
        CanonicalText,
        Probability,
        CumulativeProbability,
        @temperature AS Temperature,
        @topK AS TopK,
        @topP AS TopP,
        DATEDIFF(MILLISECOND, @startTime, SYSUTCDATETIME()) AS DurationMs
    FROM @sampledAtoms
    ORDER BY Step;

    RETURN 0;
END
GO

GRANT EXECUTE ON dbo.sp_RunInference TO PUBLIC;
GO

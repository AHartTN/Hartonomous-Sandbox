CREATE PROCEDURE dbo.sp_MultiPathReasoning
    @ProblemId UNIQUEIDENTIFIER,
    @BasePrompt NVARCHAR(MAX),
    @NumPaths INT = 3,
    @MaxDepth INT = 3,
    @BranchingFactor INT = 2,
    @Debug BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
    DECLARE @ReasoningTree TABLE (
        PathId INT,
        StepNumber INT,
        BranchId INT,
        Prompt NVARCHAR(MAX),
        Response NVARCHAR(MAX),
        Score FLOAT,
        StepTime DATETIME2
    );

    IF @Debug = 1
        PRINT 'Starting multi-path reasoning with ' + CAST(@NumPaths AS NVARCHAR(10)) + ' paths';

    DECLARE @PathId INT = 1;
    WHILE @PathId <= @NumPaths
    BEGIN
        DECLARE @CurrentPrompt NVARCHAR(MAX) = @BasePrompt;
        DECLARE @StepNumber INT = 1;

        WHILE @StepNumber <= @MaxDepth
        BEGIN
            DECLARE @StepResponse NVARCHAR(MAX);
            EXEC dbo.sp_GenerateText
                @prompt = @CurrentPrompt,
                @max_tokens = 80,
                @temperature = 0.9,
                @GeneratedText = @StepResponse OUTPUT;

            INSERT INTO @ReasoningTree (PathId, StepNumber, BranchId, Prompt, Response, Score, StepTime)
            VALUES (@PathId, @StepNumber, 1, @CurrentPrompt, @StepResponse, 0.8, SYSUTCDATETIME());

            SET @CurrentPrompt = 'Continue exploring: ' + @StepResponse;
            SET @StepNumber = @StepNumber + 1;
        END

        SET @PathId = @PathId + 1;
    END

    UPDATE @ReasoningTree
    SET Score = Score + (RAND() * 0.4 - 0.2);

    DECLARE @BestPathId INT;
    SELECT TOP 1 @BestPathId = PathId
    FROM @ReasoningTree
    GROUP BY PathId
    ORDER BY AVG(Score) DESC;

    INSERT INTO dbo.MultiPathReasoning (
        ProblemId,
        BasePrompt,
        NumPaths,
        MaxDepth,
        BestPathId,
        ReasoningTree,
        DurationMs,
        CreatedAt
    )
    VALUES (
        @ProblemId,
        @BasePrompt,
        @NumPaths,
        @MaxDepth,
        @BestPathId,
        (SELECT * FROM @ReasoningTree FOR JSON PATH),
        DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()),
        SYSUTCDATETIME()
    );

    SELECT
        @ProblemId AS ProblemId,
        'multi_path' AS ReasoningType,
        @BestPathId AS BestPathId,
        PathId,
        StepNumber,
        BranchId,
        Prompt,
        Response,
        Score,
        StepTime
    FROM @ReasoningTree
    ORDER BY PathId, StepNumber;

    IF @Debug = 1
        PRINT 'Multi-path reasoning completed, best path: ' + CAST(@BestPathId AS NVARCHAR(10));
END;
GO
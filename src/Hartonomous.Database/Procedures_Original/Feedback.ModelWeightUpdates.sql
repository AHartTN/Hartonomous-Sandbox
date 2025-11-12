CREATE OR ALTER PROCEDURE dbo.sp_UpdateModelWeightsFromFeedback
    @learningRate FLOAT = 0.001,
    @minRatings INT = 10,
    @maxRating TINYINT = 5,
    @ModelId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @layersUpdated INT = 0;
    DECLARE @eligibleLayers INT = 0;
    DECLARE @startTime DATETIME2 = SYSUTCDATETIME();
    DECLARE @endTime DATETIME2;
    DECLARE @durationMs INT;
    DECLARE @startedTransaction BIT = 0;

    IF @@TRANCOUNT = 0
    BEGIN
        SET @startedTransaction = 1;
        BEGIN TRANSACTION;
    END
    ELSE
    BEGIN
        SAVE TRANSACTION UpdateModelWeightsSavepoint;
    END;

    BEGIN TRY
        CREATE TABLE #LayerUpdates
        (
            LayerID INT,
            ModelID INT,
            LayerName NVARCHAR(100),
            SuccessfulInferences INT,
            AverageRating DECIMAL(3,2),
            UpdateMagnitude FLOAT
        );

        INSERT INTO #LayerUpdates (LayerID, ModelID, LayerName, SuccessfulInferences, AverageRating)
        SELECT
            ml.LayerId,
            ml.ModelId,
            ml.LayerName,
            COUNT(DISTINCT ir.InferenceId) AS SuccessfulInferences,
            AVG(CAST(ir.UserRating AS DECIMAL(3,2))) AS AverageRating
        FROM dbo.ModelLayers AS ml
        INNER JOIN dbo.InferenceSteps AS ist ON ml.LayerId = ist.LayerId
        INNER JOIN dbo.InferenceRequests AS ir ON ist.InferenceId = ir.InferenceId
        WHERE ir.UserRating >= 4
            AND ir.UserRating <= @maxRating
            AND (@ModelId IS NULL OR ml.ModelId = @ModelId)
        GROUP BY ml.LayerId, ml.ModelId, ml.LayerName
        HAVING COUNT(DISTINCT ir.InferenceId) >= @minRatings;

        SET @eligibleLayers = @@ROWCOUNT;

        PRINT 'Found ' + CAST(@eligibleLayers AS NVARCHAR(10)) + ' layers eligible for weight updates'
            + CASE WHEN @ModelId IS NULL THEN '' ELSE ' (ModelId = ' + CAST(@ModelId AS NVARCHAR(12)) + ')' END;

        UPDATE lu
        SET UpdateMagnitude = CASE
                                  WHEN @maxRating <= 0 THEN 0
                                  ELSE @learningRate * (lu.AverageRating / @maxRating) * SQRT(lu.SuccessfulInferences)
                              END
        FROM #LayerUpdates AS lu;

        SELECT @layersUpdated = COUNT(*) FROM #LayerUpdates;

        -- Apply weight updates using set-based operation
        UPDATE w
        SET 
            w.Value = w.Value + (u.UpdateMagnitude * @learningRate),
            w.LastUpdated = SYSUTCDATETIME(),
            w.UpdateCount = ISNULL(w.UpdateCount, 0) + 1
        FROM Weights w
        INNER JOIN #LayerUpdates u ON w.LayerID = u.LayerID
        WHERE u.UpdateMagnitude > 0;

        SET @endTime = SYSUTCDATETIME();
        SET @durationMs = DATEDIFF(MILLISECOND, @startTime, @endTime);

        PRINT 'Feedback loop completed - Updated ' + CAST(@layersUpdated AS NVARCHAR(10)) +
              ' layers in ' + CAST(@durationMs AS NVARCHAR(10)) + 'ms';

        IF @startedTransaction = 1
        BEGIN
            COMMIT TRANSACTION;
        END;

        SELECT
            @layersUpdated AS LayersUpdated,
            @learningRate AS LearningRate,
            @minRatings AS MinRatings,
            @durationMs AS DurationMs,
            @endTime AS CompletedAt;

        SELECT
            LayerID,
            ModelID,
            LayerName,
            SuccessfulInferences,
            AverageRating,
            UpdateMagnitude
        FROM #LayerUpdates
        ORDER BY UpdateMagnitude DESC;

        DROP TABLE #LayerUpdates;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() = -1
        BEGIN
            ROLLBACK TRANSACTION;
        END
        ELSE IF XACT_STATE() = 1
        BEGIN
            IF @startedTransaction = 1
            BEGIN
                ROLLBACK TRANSACTION;
            END
            ELSE
            BEGIN
                ROLLBACK TRANSACTION UpdateModelWeightsSavepoint;
            END
        END;

        THROW;
    END CATCH;

    RETURN @layersUpdated;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_UpdateModelWeightsFromFeedback
    @learningRate FLOAT = 0.001,
    @minRatings INT = 10,
    @maxRating TINYINT = 5
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @layersUpdated INT = 0;
    DECLARE @startTime DATETIME2 = SYSUTCDATETIME();

    CREATE TABLE #LayerUpdates (
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
    FROM dbo.ModelLayers ml
    INNER JOIN dbo.InferenceSteps ist ON ml.LayerId = ist.LayerId
    INNER JOIN dbo.InferenceRequests ir ON ist.InferenceId = ir.InferenceId
    WHERE ir.UserRating >= 4
        AND ir.UserRating <= @maxRating
    GROUP BY ml.LayerId, ml.ModelId, ml.LayerName
    HAVING COUNT(DISTINCT ir.InferenceId) >= @minRatings;

    PRINT 'Found ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' layers eligible for weight updates';

    DECLARE @currentLayerID INT, @currentModelID INT, @currentLayerName NVARCHAR(100);
    DECLARE @successCount INT, @avgRating DECIMAL(3,2);

    DECLARE layer_cursor CURSOR FOR
    SELECT LayerID, ModelID, LayerName, SuccessfulInferences, AverageRating
    FROM #LayerUpdates;

    OPEN layer_cursor;
    FETCH NEXT FROM layer_cursor INTO @currentLayerID, @currentModelID, @currentLayerName, @successCount, @avgRating;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @updateMagnitude FLOAT = @learningRate * (@avgRating / @maxRating) * SQRT(@successCount);

        UPDATE #LayerUpdates
        SET UpdateMagnitude = @updateMagnitude
        WHERE LayerID = @currentLayerID;

        PRINT 'Layer ' + @currentLayerName + ' (ID: ' + CAST(@currentLayerID AS NVARCHAR(10)) +
              ') - Success count: ' + CAST(@successCount AS NVARCHAR(10)) +
              ', Avg rating: ' + CAST(@avgRating AS NVARCHAR(10)) +
              ', Update magnitude: ' + CAST(@updateMagnitude AS NVARCHAR(20));

        SET @layersUpdated = @layersUpdated + 1;

        FETCH NEXT FROM layer_cursor INTO @currentLayerID, @currentModelID, @currentLayerName, @successCount, @avgRating;
    END;

    CLOSE layer_cursor;
    DEALLOCATE layer_cursor;

    DECLARE @endTime DATETIME2 = SYSUTCDATETIME();
    DECLARE @durationMs INT = DATEDIFF(MILLISECOND, @startTime, @endTime);

    PRINT 'Feedback loop completed - Updated ' + CAST(@layersUpdated AS NVARCHAR(10)) +
          ' layers in ' + CAST(@durationMs AS NVARCHAR(10)) + 'ms';

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

    RETURN @layersUpdated;
END
GO
